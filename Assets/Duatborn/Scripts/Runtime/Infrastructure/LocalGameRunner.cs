using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Duatborn.Core;

namespace Duatborn.Unity
{
    public sealed class LocalGameRunner : MonoBehaviour
    {
        public CreatureDefinitionAsset player0Definition;
        public CreatureDefinitionAsset player1Definition;
        public EnemyDefinitionAsset enemyDefinition;
        public int enemyCount = 6;
        public float arenaRadius = 18f;
        public int playerCount = 2;
        public CreatureKind selectedCreature = CreatureKind.DuneWarden;
        public bool pauseAllowed;
        public Camera arenaCamera;
        public CombatView view;
        public CombatHud hud;
        public IsometricCameraRig cameraRig;

        public CombatSimulation Simulation { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }

        private readonly List<DuatbornInputActions> _actions = new List<DuatbornInputActions>();
        private readonly List<PlayerInputAdapter> _adapters = new List<PlayerInputAdapter>();
        private const int MaxStepsPerFrame = 5;
        private float _accumulator;
        private ArenaSpec[] _rooms;
        private RunState _run;
        private CreatureSpec[] _playerSpecs;
        private bool _upgradePending;
        private readonly UpgradeField _upgradeField = new UpgradeField();
        private readonly System.Random _rng = new System.Random();

        private void Awake()
        {
            if (view == null) view = GetComponent<CombatView>();
            if (hud == null) hud = GetComponent<CombatHud>();
            if (cameraRig == null) cameraRig = GetComponent<IsometricCameraRig>();
            if (arenaCamera == null) arenaCamera = Camera.main;
        }

        public void BeginSession()
        {
            EndSession();
            if (arenaCamera == null) arenaCamera = Camera.main;

            for (int i = 0; i < playerCount; i++)
            {
                DuatbornInputActions actions = new DuatbornInputActions(i);
                actions.Enable();
                _actions.Add(actions);
                _adapters.Add(new PlayerInputAdapter(actions, arenaCamera));
            }

            _rooms = DefaultContent.DefaultRun();
            _run = new RunState();
            _run.Reset(_rooms.Length);

            BuildRoom();
            if (view != null) view.LocalPlayerId = playerCount <= 1 ? 0 : -1;
            if (cameraRig != null) cameraRig.SetLocalPlayer(playerCount <= 1 ? 0 : -1);
            IsPaused = false;
            IsRunning = true;
        }

        private ArenaSpec CurrentRoom()
        {
            int index = _run == null ? 0 : _run.RoomIndex;
            if (_rooms == null || _rooms.Length == 0) return null;
            if (index < 0) index = 0;
            if (index >= _rooms.Length) index = _rooms.Length - 1;
            return _rooms[index];
        }

        private CreatureKind[] PlayerKinds()
        {
            if (playerCount <= 1) return new[] { selectedCreature };
            CreatureKind second = selectedCreature == CreatureKind.DuneWarden ? CreatureKind.Sunwing : CreatureKind.DuneWarden;
            return new[] { selectedCreature, second };
        }

        private void EnsurePlayerSpecs()
        {
            CreatureKind[] kinds = PlayerKinds();
            if (_playerSpecs != null && _playerSpecs.Length == kinds.Length) return;
            _playerSpecs = new CreatureSpec[kinds.Length];
            for (int i = 0; i < kinds.Length; i++)
                _playerSpecs[i] = kinds[i] == CreatureKind.Sunwing ? DefaultContent.Sunwing() : DefaultContent.DuneWarden();
        }

        private void ApplyUpgradeTo(int playerIndex, int upgradeIndex)
        {
            UpgradeSpec[] catalog = DefaultContent.UpgradeCatalog();
            if (upgradeIndex < 0 || upgradeIndex >= catalog.Length) return;
            EnsurePlayerSpecs();
            if (playerIndex < 0 || playerIndex >= _playerSpecs.Length) return;
            UpgradeRules.Apply(catalog[upgradeIndex], _playerSpecs[playerIndex]);
        }

        private void SpawnUpgrades()
        {
            Vec2[] positions = DefaultContent.UpgradePositions();
            int[] indices = DefaultContent.PickUpgradeIndices(positions.Length, PlayerKinds(), _rng);
            _upgradeField.Spawn(positions, indices);
            _upgradePending = true;
            if (view != null) view.SetPickups(_upgradeField.Pickups);
        }

        private bool TryCollectPickup(out int collector, out int upgradeIndex)
        {
            collector = -1;
            upgradeIndex = -1;
            for (int i = 0; i < _actions.Count; i++)
            {
                if (_actions[i] == null || !_actions[i].Interact.IsPressed()) continue;
                PlayerState p = Simulation.GetPlayer(i);
                if (p == null || p.IsDefeated) continue;
                int index;
                int id;
                if (!_upgradeField.TryCollectAt(p.Position, 2.0f, out index, out id)) continue;
                collector = i;
                upgradeIndex = index;
                return true;
            }
            return false;
        }

        private void BuildRoom()
        {
            EnsurePlayerSpecs();
            _upgradeField.Clear();
            if (view != null) view.SetPickups(_upgradeField.Pickups);
            ArenaSpec room = CurrentRoom();
            CombatSetup setup = CombatSetup.FromArena(_playerSpecs, room, true);
            setup.ArenaRadius = arenaRadius;
            if (enemyDefinition != null) setup.EnemySpec = enemyDefinition.spec;
            Simulation = new CombatSimulation(setup);
            if (view != null) view.Bind(Simulation);
            if (hud != null)
            {
                hud.Bind(Simulation);
                hud.RoomIndex = _run.RoomIndex;
                hud.RoomCount = _run.RoomCount;
                hud.RoomName = room != null ? room.DisplayName : string.Empty;
                hud.RunComplete = _run.RunComplete;
                hud.RunFailed = _run.RunFailed;
                hud.UpgradePending = _upgradePending;
                hud.UpgradeOptions = UpgradeOptionLabels();
            }
            if (cameraRig != null) cameraRig.Bind(Simulation);
            _accumulator = 0f;
        }

        private string[] UpgradeOptionLabels()
        {
            UpgradeSpec[] catalog = DefaultContent.UpgradeCatalog();
            string[] labels = new string[catalog.Length];
            for (int i = 0; i < catalog.Length; i++) labels[i] = catalog[i].DisplayName;
            return labels;
        }



        public void SetPaused(bool paused)
        {
            if (!pauseAllowed) return;
            IsPaused = paused;
        }

        public void Restart()
        {
            if (_run == null) return;
            _run.Reset(_rooms.Length);
            _playerSpecs = null;
            _upgradePending = false;
            BuildRoom();
            IsPaused = false;
        }

        public void EndSession()
        {
            for (int i = 0; i < _actions.Count; i++) _actions[i].Dispose();
            _actions.Clear();
            _adapters.Clear();
            IsRunning = false;
            IsPaused = false;
            _playerSpecs = null;
            _upgradePending = false;
            Simulation = null;
        }

        private void Update()
        {
            if (!IsRunning || Simulation == null) return;

            if (pauseAllowed && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                IsPaused = !IsPaused;

            if (hud != null) hud.Paused = IsPaused;

            if (IsPaused)
            {
                if (view != null) view.Render(Simulation, Time.deltaTime);
                return;
            }

            for (int i = 0; i < _adapters.Count; i++)
                _adapters[i].CaptureFrame(_actions[i].Dodge.WasPressedThisFrame());

            float fixedDt = Simulation.DeltaTime;
            _accumulator += Time.deltaTime;
            int steps = 0;
            while (_accumulator >= fixedDt && steps < MaxStepsPerFrame)
            {
                for (int i = 0; i < _adapters.Count; i++)
                {
                    PlayerState p = Simulation.GetPlayer(i);
                    Vector3 world = p != null ? ArenaSpace.ToWorld(p.Position) : Vector3.zero;
                    Simulation.SubmitCommand(i, _adapters[i].Build(world));
                }
                Simulation.Step();
                if (view != null && Simulation.Events.Count > 0) view.ConsumeEvents(Simulation.Events);
                _accumulator -= fixedDt;
                steps++;
            }

            if (view != null)
            {
                view.SetPickups(_upgradeField.Pickups);
                view.Render(Simulation, Time.deltaTime);
            }

            if (_run != null)
            {
                if (_upgradePending)
                {
                    int collector;
                    int upgradeIndex;
                    if (TryCollectPickup(out collector, out upgradeIndex))
                    {
                        ApplyUpgradeTo(collector, upgradeIndex);
                        _upgradePending = false;
                        _run.Update(true, false);
                        BuildRoom();
                    }
                }
                else if (Simulation.AllPlayersDefeated)
                {
                    _run.Update(false, true);
                }
                else if (Simulation.EncounterCleared)
                {
                    if (_run.IsFinalRoom) _run.Update(true, false);
                    else SpawnUpgrades();
                }

                if (hud != null)
                {
                    hud.RoomIndex = _run.RoomIndex;
                    hud.RoomCount = _run.RoomCount;
                    ArenaSpec room = CurrentRoom();
                    hud.RoomName = room != null ? room.DisplayName : string.Empty;
                    hud.RunComplete = _run.RunComplete;
                    hud.RunFailed = _run.RunFailed;
                    hud.UpgradePending = _upgradePending;
                    hud.UpgradeOptions = UpgradeOptionLabels();
                }

                bool finished = _run.RunComplete || _run.RunFailed;
                if (finished && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                    Restart();
            }
        }

        private void OnDestroy()
        {
            EndSession();
        }
    }
}
