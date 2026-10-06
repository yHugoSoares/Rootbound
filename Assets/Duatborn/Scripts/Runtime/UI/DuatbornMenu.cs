using UnityEngine;
using Duatborn.Core;

namespace Duatborn.Unity
{
    public sealed class DuatbornMenu : MonoBehaviour
    {
        public LocalGameRunner runner;
        public int localPlayerCount = 2;

        private enum StartMode
        {
            None,
            Solo,
            LocalCoop,
            HostCoop,
            JoinCoop
        }

        private readonly OfflineNetworkSession _offline = new OfflineNetworkSession();
        private INetworkSession _session;
        private INetworkSession _online;

        private StartMode _pendingMode;
        private CreatureKind _soloCreature = CreatureKind.DuneWarden;
        private string _joinCode = string.Empty;
        private bool _started;
        private bool _soloRequested;
        private bool _localCoopRequested;
        private bool _hostRequested;
        private bool _joinRequested;
        private bool _returnRequested;
        private GUIStyle _title;
        private GUIStyle _label;

        private void Awake()
        {
            if (runner == null) runner = FindFirstObjectByType<LocalGameRunner>();
            _session = _offline;
        }

        private void Update()
        {
            if (_returnRequested)
            {
                _returnRequested = false;
                ReturnToMenu();
            }

            if (_soloRequested)
            {
                _soloRequested = false;
                BeginMode(_offline, StartMode.Solo, true, null);
            }

            if (_localCoopRequested)
            {
                _localCoopRequested = false;
                BeginMode(_offline, StartMode.LocalCoop, true, null);
            }

            if (_hostRequested)
            {
                _hostRequested = false;
                BeginOnline(StartMode.HostCoop);
            }

            if (_joinRequested)
            {
                _joinRequested = false;
                BeginOnlineJoin();
            }

            if (!_started && (_session.State == NetworkSessionState.Hosting || _session.State == NetworkSessionState.Connected))
                StartArena();

            if (_started && (_session.State == NetworkSessionState.Disconnected || _session.State == NetworkSessionState.Error))
                ReturnToMenu();
        }

        private void BeginMode(INetworkSession session, StartMode mode, bool host, string code)
        {
            if (_session != null) _session.Leave();
            _session = session;
            _session.LocalCreature = _soloCreature;
            _pendingMode = mode;
            if (host) _session.StartHost();
            else _session.Join(code);
        }

        private void BeginOnline(StartMode mode)
        {
            INetworkSession online = GetOnline();
            if (online != null) BeginMode(online, mode, true, null);
        }

        private void BeginOnlineJoin()
        {
            if (string.IsNullOrEmpty(_joinCode)) return;
            INetworkSession online = GetOnline();
            if (online != null) BeginMode(online, StartMode.JoinCoop, false, _joinCode);
        }

        private INetworkSession GetOnline()
        {
            if (_online == null)
            {
                _online = NetworkSessionFactory.New();
                if (_online is OfflineNetworkSession)
                {
                    _online = null;
                    _offline.Join("fusion-unavailable");
                    return null;
                }
            }
            return _online;
        }

        private void StartArena()
        {
            bool offline = _pendingMode == StartMode.Solo || _pendingMode == StartMode.LocalCoop;
            if (offline && runner != null)
            {
                runner.playerCount = _pendingMode == StartMode.Solo ? 1 : localPlayerCount;
                runner.selectedCreature = _soloCreature;
                runner.pauseAllowed = true;
                runner.BeginSession();
            }
            _started = true;
        }

        public void ReturnToMenu()
        {
            if (runner != null) runner.EndSession();
            if (_session != null) _session.Leave();
            _session = _offline;
            _pendingMode = StartMode.None;
            _started = false;
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (_started) DrawStatus();
            else DrawMenu();
        }

        private void DrawMenu()
        {
            float width = 520f;
            Rect panel = new Rect(Screen.width * 0.5f - width * 0.5f, Screen.height * 0.5f - 230f, width, 460f);
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;

            GUI.Label(new Rect(panel.x + 20f, panel.y + 16f, width - 40f, 34f), "DUATBORN", _title);

            GUI.Label(new Rect(panel.x + 20f, panel.y + 56f, width - 40f, 22f), "Your creature (Solo / Host / Join)", _label);
            if (GUI.Button(new Rect(panel.x + 20f, panel.y + 80f, 160f, 30f),
                    (_soloCreature == CreatureKind.DuneWarden ? "> " : "") + "Dune Warden"))
                _soloCreature = CreatureKind.DuneWarden;
            if (GUI.Button(new Rect(panel.x + 188f, panel.y + 80f, 160f, 30f),
                    (_soloCreature == CreatureKind.Sunwing ? "> " : "") + "Sunwing"))
                _soloCreature = CreatureKind.Sunwing;

            bool busy = _session.State == NetworkSessionState.Starting;
            GUI.enabled = !busy;

            if (GUI.Button(new Rect(panel.x + 20f, panel.y + 118f, width - 40f, 34f), "Play Solo"))
                _soloRequested = true;

            GUI.Label(new Rect(panel.x + 20f, panel.y + 168f, width - 40f, 22f), "Player-Hosted Co-op (Fusion, online)", _label);
            if (GUI.Button(new Rect(panel.x + 20f, panel.y + 192f, width - 40f, 34f), "Host Co-op"))
                _hostRequested = true;
            _joinCode = GUI.TextField(new Rect(panel.x + 20f, panel.y + 238f, width - 130f, 30f), _joinCode);
            if (GUI.Button(new Rect(panel.x + width - 100f, panel.y + 238f, 80f, 30f), "Join Co-op"))
                _joinRequested = true;

            GUI.Label(new Rect(panel.x + 20f, panel.y + 286f, width - 40f, 22f), "Local Co-op (same screen, 2 players)", _label);
            if (GUI.Button(new Rect(panel.x + 20f, panel.y + 310f, width - 40f, 34f), "Local Co-op (2 players)"))
                _localCoopRequested = true;

            GUI.enabled = true;

            string code = string.IsNullOrEmpty(_session.SessionCode) ? string.Empty : "   Code: " + _session.SessionCode;
            GUI.Label(new Rect(panel.x + 20f, panel.y + 356f, width - 40f, 22f), "Status: " + _session.State + code, _label);

            if (!string.IsNullOrEmpty(_session.LastError))
                GUI.Label(new Rect(panel.x + 20f, panel.y + 380f, width - 40f, 40f), _session.LastError, _label);

            if (GUI.Button(new Rect(panel.x + 20f, panel.y + 418f, 120f, 30f), "Quit"))
                Application.Quit();
        }

        private void DrawStatus()
        {
            string mode = _session == _offline ? "offline" : "Fusion";
            string status = _session.IsHost ? "hosting " + _session.SessionCode : "client " + _session.State;
            GUI.Label(new Rect(16f, Screen.height - 28f, 700f, 22f), "Session [" + mode + "]: " + status, _label);

            if (runner != null && runner.pauseAllowed)
                GUI.Label(new Rect(Screen.width - 240f, Screen.height - 28f, 224f, 22f),
                    runner.IsPaused ? "PAUSED (Esc)" : "Esc to pause", _label);

            if (GUI.Button(new Rect(16f, Screen.height - 58f, 160f, 24f), "Return to Menu"))
                _returnRequested = true;
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label);
            _title.fontSize = 22;
            _title.normal.textColor = new Color(0.85f, 0.95f, 0.8f);
            _label = new GUIStyle(GUI.skin.label);
            _label.fontSize = 14;
            _label.normal.textColor = Color.white;
        }
    }
}
