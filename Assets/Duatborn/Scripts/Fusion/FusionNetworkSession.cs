using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using Duatborn.Core;
using Duatborn.Unity;

namespace Duatborn.Fusion
{
    public sealed class FusionNetworkSession : MonoBehaviour, INetworkSession, INetworkRunnerCallbacks
    {
        public NetworkSessionState State { get; private set; } = NetworkSessionState.Offline;
        public bool IsHost { get; private set; }
        public string SessionCode { get; private set; } = string.Empty;
        public string LastError { get; private set; } = string.Empty;
        public int PlayerCount { get; private set; }
        public CreatureKind LocalCreature { get; set; } = CreatureKind.DuneWarden;

        public event Action Changed;

        private NetworkRunner _runner;
        private bool _busy;
        private DuatbornInputActions _actions;
        private PlayerInputAdapter _adapter;
        private FusionCombatHost _localHost;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            NetworkSessionFactory.Create = () =>
            {
                GameObject go = new GameObject("FusionNetworkSession");
                DontDestroyOnLoad(go);
                return go.AddComponent<FusionNetworkSession>();
            };
        }

        public void StartHost()
        {
            if (!CanStart()) return;
            IsHost = true;
            SessionCode = GenerateCode();
            StartRunner(GameMode.Host, SessionCode);
        }

        public void Join(string code)
        {
            if (!CanStart()) return;
            if (string.IsNullOrWhiteSpace(code))
            {
                Fail("Enter a session code.");
                return;
            }
            IsHost = false;
            SessionCode = code.Trim().ToUpperInvariant();
            StartRunner(GameMode.Client, SessionCode);
        }

        public void Leave()
        {
            if (_runner != null)
            {
                _runner.Shutdown();
                Destroy(_runner.gameObject);
                _runner = null;
            }
            DisposeLocalInput();
            _busy = false;
            PlayerCount = 0;
            State = NetworkSessionState.Offline;
            Notify();
        }

        private bool CanStart()
        {
            return !_busy
                && State != NetworkSessionState.Starting
                && State != NetworkSessionState.Connected
                && State != NetworkSessionState.Hosting;
        }

        private void StartRunner(GameMode mode, string sessionName)
        {
            if (!CanStart()) return;
            _busy = true;
            State = NetworkSessionState.Starting;
            LastError = string.Empty;
            Notify();

            EnsureRunner();

            INetworkSceneManager sceneManager = _runner.GetComponent<INetworkSceneManager>();
            if (sceneManager == null) sceneManager = _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

            INetworkObjectProvider objectProvider = _runner.GetComponent<INetworkObjectProvider>();
            if (objectProvider == null) objectProvider = _runner.gameObject.AddComponent<NetworkObjectProviderDefault>();

            _runner.StartGame(new StartGameArgs
            {
                GameMode = mode,
                SessionName = sessionName,
                SceneManager = sceneManager,
                ObjectProvider = objectProvider,
                OnGameStarted = HandleGameStarted,
            });
        }

        private void HandleGameStarted(NetworkRunner runner)
        {
            _busy = false;
            PlayerCount = CountPlayers(runner);
            State = NetworkSessionState.Connected;
            EnsureLocalInput();
            Notify();

            if (runner.IsServer)
                runner.Spawn("FusionMatch", Vector3.zero, Quaternion.identity);
        }

        private void Update()
        {
            if (_actions == null || _adapter == null) return;
            _adapter.CaptureFrame(_actions.Dodge.WasPressedThisFrame());
        }

        private void EnsureLocalInput()
        {
            if (_actions != null) return;
            _actions = new DuatbornInputActions(0);
            _actions.Enable();
            _adapter = new PlayerInputAdapter(_actions, Camera.main);
        }

        private void DisposeLocalInput()
        {
            if (_actions != null)
            {
                _actions.Dispose();
                _actions = null;
                _adapter = null;
            }
        }

        private void EnsureRunner()
        {
            if (_runner != null && !_runner.IsRunning)
            {
                Destroy(_runner.gameObject);
                _runner = null;
            }

            if (_runner != null) return;
            GameObject go = new GameObject("FusionNetworkRunner");
            DontDestroyOnLoad(go);
            _runner = go.AddComponent<NetworkRunner>();
            _runner.AddCallbacks(this);
            _runner.ProvideInput = true;
        }

        private void Fail(string message)
        {
            LastError = message;
            State = NetworkSessionState.Error;
            Notify();
        }

        private void Notify()
        {
            if (Changed != null) Changed();
        }

        private static string GenerateCode()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            char[] chars = new char[4];
            for (int i = 0; i < chars.Length; i++)
                chars[i] = alphabet[UnityEngine.Random.Range(0, alphabet.Length)];
            return "RB-" + new string(chars);
        }

        private static int CountPlayers(NetworkRunner runner)
        {
            int count = 0;
            foreach (PlayerRef _ in runner.ActivePlayers) count++;
            return count;
        }

        private void OnDestroy()
        {
            DisposeLocalInput();
            if (_runner != null) _runner.Shutdown();
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            PlayerCount = CountPlayers(runner);
            Notify();
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            PlayerCount = CountPlayers(runner);
            Notify();
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            _busy = false;
            PlayerCount = CountPlayers(runner);
            State = NetworkSessionState.Connected;
            Notify();
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            _busy = false;
            LastError = "Disconnected: " + reason;
            State = NetworkSessionState.Disconnected;
            Notify();
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress netAddress, NetConnectFailedReason reason)
        {
            _busy = false;
            Fail("Connection failed: " + reason);
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            _busy = false;
            PlayerCount = 0;
            if (State == NetworkSessionState.Starting)
            {
                LastError = "Start failed: " + shutdownReason;
                State = NetworkSessionState.Error;
            }
            else
            {
                State = NetworkSessionState.Offline;
            }
            Notify();
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            EnsureLocalInput();

            if (_localHost == null || _localHost.Runner != runner)
                _localHost = FindLocalHost(runner);

            PlayerCommand cmd;
            if (_localHost != null && _localHost.HasLocalCommandOverride)
            {
                cmd = _localHost.LocalCommandOverride;
            }
            else if (_adapter != null)
            {
                Vector3 world = _localHost != null ? _localHost.LocalWorldPosition : Vector3.zero;
                cmd = _adapter.Build(world);
            }
            else
            {
                cmd = default(PlayerCommand);
            }

            DuatbornInput value = default(DuatbornInput);
            value.Move = new Vector2(cmd.Move.X, cmd.Move.Y);
            value.Aim = new Vector2(cmd.Aim.X, cmd.Aim.Y);
            value.TargetPoint = new Vector2(cmd.TargetPoint.X, cmd.TargetPoint.Y);
            value.HasTargetPoint = cmd.HasTargetPoint;
            value.Primary = cmd.Primary;
            value.Special = cmd.Special;
            value.Dodge = cmd.Dodge;
            value.Interact = cmd.Interact;
            value.Creature = (byte)LocalCreature;
            value.Restart = UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame;
            input.Set(value);
        }

        private static FusionCombatHost FindLocalHost(NetworkRunner runner)
        {
            FusionCombatHost[] all = UnityEngine.Object.FindObjectsByType<FusionCombatHost>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
                if (all[i].Runner == runner) return all[i];
            return null;
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
    }
}
