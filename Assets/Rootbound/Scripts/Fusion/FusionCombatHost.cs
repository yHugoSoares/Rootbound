using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using Rootbound.Core;
using Rootbound.Unity;

namespace Rootbound.Fusion
{
    [System.Serializable]
    public struct PlayerNetData : INetworkStruct
    {
        public Vector3 Position;
        public Vector3 Facing;
        public float Health;
        public float MaxHealth;
        public NetworkBool Defeated;
        public Vector3 AimTarget;
        public NetworkBool HasAimTarget;
        public NetworkBool AimTargetClamped;
    }

    [System.Serializable]
    public struct EnemyNetData : INetworkStruct
    {
        public Vector3 Position;
        public float Health;
        public NetworkBool Defeated;
    }

    [System.Serializable]
    public struct CageNetData : INetworkStruct
    {
        public Vector3 Position;
        public float Radius;
        public float Remaining;
        public NetworkBool Ignited;
    }

    [System.Serializable]
    public struct PickupNetData : INetworkStruct
    {
        public Vector3 Position;
        public byte UpgradeIndex;
        public NetworkBool Collected;
    }

    // Host-authoritative match. The host owns the single CombatSimulation and
    // replicates player state, enemies and cages; clients apply it to a local
    // mirror. The lobby starts with only the connected players (no enemies) and
    // the host starts the encounter with R. Each player picks their own creature.
    public sealed class FusionCombatHost : NetworkBehaviour
    {
        private const int MaxPlayers = 2;
        private const int MaxEnemies = 12;

        public static FusionCombatHost LocalInstance;

        [Networked] private int PlayerCountNet { get; set; }

        [Networked, Capacity(MaxPlayers)]
        private NetworkArray<byte> PlayerCreatures => default;

        [Networked] private NetworkBool EncounterStarted { get; set; }

        [Networked] private int RoomIndex { get; set; }

        [Networked] private NetworkBool RunComplete { get; set; }

        [Networked] private NetworkBool RunFailed { get; set; }

        [Networked] private NetworkBool UpgradePending { get; set; }

        [Networked, Capacity(3)]
        private NetworkArray<PickupNetData> PickupsData => default;

        [Networked] private int PickupCount { get; set; }

        [Networked, Capacity(MaxPlayers)]
        private NetworkArray<PlayerNetData> PlayersData => default;

        [Networked, Capacity(MaxEnemies)]
        private NetworkArray<EnemyNetData> EnemiesData => default;

        [Networked] private int EnemyCount { get; set; }

        [Networked, Capacity(4)]
        private NetworkArray<CageNetData> CagesData => default;

        [Networked] private int CageCount { get; set; }

        public CombatSimulation Simulation { get { return _sim; } }
        public bool IsEncounterStarted { get { return EncounterStarted; } }
        public bool IsUpgradePending { get { return UpgradePending; } }
        public int CurrentRoomIndex { get { return RoomIndex; } }
        public Vector3 NetPlayer0Position { get { return PlayersData[0].Position; } }

        public void ChooseUpgrade(int index)
        {
            if (!HasStateAuthority) return;
            ApplyUpgrade(index);
            UpgradePending = false;
            ClearPickups();
            if (RoomIndex < _rooms.Length - 1) RoomIndex++;
            EnsureSimulation();
        }

        private void SpawnUpgrades()
        {
            Vec2[] positions = DefaultContent.UpgradePositions();
            int[] indices = DefaultContent.PickUpgradeIndices(positions.Length, RosterCreatures(), _rng);
            _upgradeField.Spawn(positions, indices);
            PickupCount = indices.Length;
            for (int i = 0; i < indices.Length; i++)
            {
                PickupNetData d = default(PickupNetData);
                d.Position = ArenaSpace.ToWorld(positions[i]);
                d.UpgradeIndex = (byte)indices[i];
                d.Collected = false;
                PickupsData.Set(i, d);
            }
            UpgradePending = true;
        }

        private void MarkPickupCollected(int pickupId)
        {
            for (int i = 0; i < _upgradeField.Pickups.Count && i < 3; i++)
            {
                if (_upgradeField.Pickups[i].Id != pickupId) continue;
                PickupNetData d = PickupsData[i];
                d.Collected = true;
                PickupsData.Set(i, d);
                break;
            }
        }

        private void ClearPickups()
        {
            _upgradeField.Clear();
            PickupCount = 0;
        }

        private void ApplyUpgradeTo(int playerIndex, int upgradeIndex)
        {
            UpgradeSpec[] catalog = DefaultContent.UpgradeCatalog();
            if (upgradeIndex < 0 || upgradeIndex >= catalog.Length) return;
            int count = PlayerCountNet < 1 ? 1 : PlayerCountNet;
            CreatureSpec[] specs = EnsurePlayerSpecs(count);
            if (playerIndex < 0 || playerIndex >= specs.Length) return;
            UpgradeRules.Apply(catalog[upgradeIndex], specs[playerIndex]);
        }

        private void BuildPickupMirror()
        {
            _pickupMirror.Clear();
            int count = PickupCount;
            if (count > 3) count = 3;
            for (int i = 0; i < count; i++)
            {
                PickupNetData d = PickupsData[i];
                UpgradePickupState p = new UpgradePickupState();
                p.Id = i + 1;
                p.Position = ArenaSpace.ToPlanar(d.Position);
                p.UpgradeIndex = d.UpgradeIndex;
                p.Collected = d.Collected;
                _pickupMirror.Add(p);
            }
        }

        public bool HasHostCommandOverride;
        public PlayerCommand HostCommandOverride;
        public bool HasHostCommandOverride2;
        public PlayerCommand HostCommandOverride2;
        public bool HasLocalCommandOverride;
        public PlayerCommand LocalCommandOverride;
        public int WriteTicks;
        public int ReadTicks;

        private CombatSimulation _sim;
        private CombatView _view;
        private CombatHud _hud;
        private IsometricCameraRig _cameraRig;
        private readonly List<PlayerRef> _refs = new List<PlayerRef>();
        private readonly byte[] _builtCreatures = new byte[MaxPlayers];
        private int _builtPlayerCount = -1;
        private int _builtEnemyCount = -1;
        private int _builtRoomIndex = -1;
        private bool _builtStarted;
        private bool _restartRequested;
        private ArenaSpec[] _rooms;
        private CreatureSpec[] _playerSpecs;
        private readonly byte[] _playerSpecsCreatures = new byte[MaxPlayers];
        private readonly UpgradeField _upgradeField = new UpgradeField();
        private readonly List<UpgradePickupState> _pickupMirror = new List<UpgradePickupState>();
        private readonly bool[] _interactThisTick = new bool[MaxPlayers];
        private readonly System.Random _rng = new System.Random();

        public Vector3 LocalWorldPosition
        {
            get
            {
                if (_sim == null) return Vector3.zero;
                PlayerState p = _sim.GetPlayer(LocalIndex());
                return p != null ? ArenaSpace.ToWorld(p.Position) : Vector3.zero;
            }
        }

        public override void Spawned()
        {
            LocalInstance = this;
            _rooms = DefaultContent.DefaultRun();
            _view = FindFirstObjectByType<CombatView>();
            _hud = FindFirstObjectByType<CombatHud>();
            _cameraRig = FindFirstObjectByType<IsometricCameraRig>();
            EnsureSimulation();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (LocalInstance == this) LocalInstance = null;
        }

        public void StartEncounter()
        {
            if (!HasStateAuthority) return;
            EncounterStarted = true;
            EnsureSimulation();
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || _sim == null) return;

            UpdateRoster();
            EnsureSimulation();

            bool finished = RunComplete || RunFailed;
            BuildRefs();
            int count = _sim.Players.Count;
            for (int i = 0; i < count; i++)
            {
                PlayerCommand cmd = default(PlayerCommand);
                if (i == 0 && HasHostCommandOverride)
                {
                    cmd = HostCommandOverride;
                }
                else if (i == 1 && HasHostCommandOverride2)
                {
                    cmd = HostCommandOverride2;
                }
                else if (i < _refs.Count)
                {
                    RootboundInput input;
                    if (Runner.TryGetInputForPlayer(_refs[i], out input))
                    {
                        cmd = ToCommand(input);
                        if (input.Restart && finished) _restartRequested = true;
                    }
                }
                _interactThisTick[i] = cmd.Interact;
                _sim.SubmitCommand(i, cmd);
            }

            if ((!EncounterStarted || finished) && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                _restartRequested = true;

            if (_restartRequested)
            {
                _restartRequested = false;
                if (RunComplete || RunFailed)
                {
                    RunComplete = false;
                    RunFailed = false;
                    RoomIndex = 0;
                }
                UpgradePending = false;
                ClearPickups();
                _playerSpecs = null;
                EncounterStarted = true;
                EnsureSimulation();
            }

            _sim.Step();
            WriteState();

            if (!RunComplete && !RunFailed)
            {
                if (_sim.AllPlayersDefeated)
                {
                    RunFailed = true;
                    UpgradePending = false;
                }
                else if (_sim.EncounterCleared && !UpgradePending)
                {
                    if (RoomIndex < _rooms.Length - 1) SpawnUpgrades();
                    else RunComplete = true;
                }
            }

            if (UpgradePending)
            {
                for (int i = 0; i < _sim.Players.Count; i++)
                {
                    if (!_interactThisTick[i]) continue;
                    PlayerState p = _sim.GetPlayer(i);
                    if (p == null || p.IsDefeated) continue;
                    int upgradeIndex;
                    int pickupId;
                    if (!_upgradeField.TryCollectAt(p.Position, 2.0f, out upgradeIndex, out pickupId)) continue;
                    ApplyUpgradeTo(i, upgradeIndex);
                    MarkPickupCollected(pickupId);
                    UpgradePending = false;
                    if (RoomIndex < _rooms.Length - 1) RoomIndex++;
                    ClearPickups();
                    EnsureSimulation();
                    break;
                }
            }
        }

        public override void Render()
        {
            if (_sim == null) return;

            if (!HasStateAuthority)
            {
                EnsureSimulation();
                ReadState();
            }

            if (_cameraRig != null) _cameraRig.localPlayerId = LocalIndex();
            BuildPickupMirror();
            if (_view != null)
            {
                _view.LocalPlayerId = LocalIndex();
                _view.SetPickups(_pickupMirror);
                _view.Render(_sim, Time.deltaTime);
            }
            if (_hud != null)
            {
                _hud.Paused = false;
                _hud.LocalPlayerId = LocalIndex();
                _hud.WaitingForStart = !EncounterStarted;
                _hud.WaitingLabel = HasStateAuthority
                    ? "Waiting for the party - press R to break the first seal"
                    : "Waiting for the host to break the first seal";
                if (_rooms != null && _rooms.Length > 0)
                {
                    int ri = Mathf.Clamp(RoomIndex, 0, _rooms.Length - 1);
                    _hud.RoomIndex = ri;
                    _hud.RoomCount = _rooms.Length;
                    _hud.RoomName = _rooms[ri].DisplayName;
                }
                _hud.RunComplete = RunComplete;
                _hud.RunFailed = RunFailed;
                _hud.UpgradePending = UpgradePending;
                _hud.UpgradeOptions = UpgradeOptionLabels();
            }
        }

        private void UpdateRoster()
        {
            BuildRefs();
            int count = _refs.Count;
            if (count < 1) count = 1;
            if (count > MaxPlayers) count = MaxPlayers;
            if (count != PlayerCountNet) PlayerCountNet = count;

            for (int i = 0; i < count; i++)
            {
                byte creature = PlayerCreatures[i];
                if (i < _refs.Count)
                {
                    RootboundInput input;
                    if (Runner.TryGetInputForPlayer(_refs[i], out input)) creature = input.Creature;
                }
                if (PlayerCreatures[i] != creature) PlayerCreatures.Set(i, creature);
            }
        }

        private void EnsureSimulation()
        {
            if (_rooms == null || _rooms.Length == 0) _rooms = DefaultContent.DefaultRun();

            int desiredCount = PlayerCountNet;
            if (desiredCount < 1) desiredCount = 1;
            if (desiredCount > MaxPlayers) desiredCount = 2;

            int roomIndex = RoomIndex;
            if (roomIndex < 0) roomIndex = 0;
            if (roomIndex >= _rooms.Length) roomIndex = _rooms.Length - 1;
            ArenaSpec room = _rooms[roomIndex];

            bool started = EncounterStarted;
            int enemies = started ? room.EnemyCount : 0;

            bool creaturesChanged = false;
            for (int i = 0; i < desiredCount; i++)
                if (_builtCreatures[i] != PlayerCreatures[i]) creaturesChanged = true;

            bool changed = _sim == null
                || _builtPlayerCount != desiredCount
                || _builtStarted != started
                || _builtEnemyCount != enemies
                || _builtRoomIndex != roomIndex
                || creaturesChanged;
            if (!changed) return;

            CreatureSpec[] specs = HasStateAuthority ? EnsurePlayerSpecs(desiredCount) : BuildSpecs(desiredCount);
            for (int i = 0; i < desiredCount; i++) _builtCreatures[i] = PlayerCreatures[i];

            _sim = new CombatSimulation(CombatSetup.FromArena(specs, room, started));
            _builtPlayerCount = desiredCount;
            _builtStarted = started;
            _builtEnemyCount = enemies;
            _builtRoomIndex = roomIndex;
            RebindView();
        }

        private CreatureKind[] RosterCreatures()
        {
            int count = PlayerCountNet < 1 ? 1 : PlayerCountNet;
            if (count > MaxPlayers) count = 2;
            CreatureKind[] kinds = new CreatureKind[count];
            for (int i = 0; i < count; i++) kinds[i] = (CreatureKind)PlayerCreatures[i];
            return kinds;
        }

        private CreatureSpec[] EnsurePlayerSpecs(int count)
        {
            bool changed = _playerSpecs == null || _playerSpecs.Length != count;
            if (!changed)
            {
                for (int i = 0; i < count; i++)
                    if (_playerSpecsCreatures[i] != PlayerCreatures[i]) { changed = true; break; }
            }
            if (changed)
            {
                _playerSpecs = BuildSpecs(count);
                for (int i = 0; i < count; i++) _playerSpecsCreatures[i] = PlayerCreatures[i];
            }
            return _playerSpecs;
        }

        private CreatureSpec[] BuildSpecs(int count)
        {
            CreatureSpec[] specs = new CreatureSpec[count];
            for (int i = 0; i < count; i++)
            {
                CreatureKind kind = (CreatureKind)PlayerCreatures[i];
                specs[i] = kind == CreatureKind.EmberMoth ? DefaultContent.EmberMoth() : DefaultContent.RootGuardian();
            }
            return specs;
        }

        private void ApplyUpgrade(int index)
        {
            UpgradeSpec[] catalog = DefaultContent.UpgradeCatalog();
            if (index < 0 || index >= catalog.Length) return;
            int count = PlayerCountNet < 1 ? 1 : PlayerCountNet;
            CreatureSpec[] specs = EnsurePlayerSpecs(count);
            for (int i = 0; i < specs.Length; i++) UpgradeRules.Apply(catalog[index], specs[i]);
        }

        private static int ReadUpgradeChoice()
        {
            if (Keyboard.current == null) return -1;
            if (Keyboard.current.digit1Key.wasPressedThisFrame) return 0;
            if (Keyboard.current.digit2Key.wasPressedThisFrame) return 1;
            if (Keyboard.current.digit3Key.wasPressedThisFrame) return 2;
            return -1;
        }

        private static string[] UpgradeOptionLabels()
        {
            UpgradeSpec[] catalog = DefaultContent.UpgradeCatalog();
            string[] labels = new string[catalog.Length];
            for (int i = 0; i < catalog.Length; i++) labels[i] = catalog[i].DisplayName;
            return labels;
        }

        private void RebindView()
        {
            if (_view == null) _view = FindFirstObjectByType<CombatView>();
            if (_hud == null) _hud = FindFirstObjectByType<CombatHud>();
            if (_cameraRig == null) _cameraRig = FindFirstObjectByType<IsometricCameraRig>();
            if (_view != null) _view.Bind(_sim);
            if (_hud != null) _hud.Bind(_sim);
            if (_cameraRig != null) _cameraRig.Bind(_sim);
        }

        private void WriteState()
        {
            WriteTicks++;

            int playerCount = _sim.Players.Count;
            for (int i = 0; i < playerCount && i < MaxPlayers; i++)
            {
                PlayerState p = _sim.Players[i];
                PlayerNetData d = default(PlayerNetData);
                d.Position = ArenaSpace.ToWorld(p.Position);
                d.Facing = ArenaSpace.ToWorld(p.Facing);
                d.Health = p.Health.Current;
                d.MaxHealth = p.Health.Max;
                d.Defeated = p.Health.IsDefeated;
                d.AimTarget = ArenaSpace.ToWorld(p.AimTarget);
                d.HasAimTarget = p.HasAimTarget;
                d.AimTargetClamped = p.AimTargetClamped;
                PlayersData.Set(i, d);
            }

            int enemyCount = _sim.Enemies.Count;
            EnemyCount = enemyCount;
            for (int i = 0; i < enemyCount && i < MaxEnemies; i++)
            {
                EnemyState e = _sim.Enemies[i];
                EnemyNetData d = default(EnemyNetData);
                d.Position = ArenaSpace.ToWorld(e.Position);
                d.Health = e.Health.Current;
                d.Defeated = e.Health.IsDefeated;
                EnemiesData.Set(i, d);
            }

            int cageCount = _sim.Cages.Count;
            CageCount = cageCount;
            for (int i = 0; i < cageCount && i < 4; i++)
            {
                RootCageState c = _sim.Cages[i];
                CageNetData d = default(CageNetData);
                d.Position = ArenaSpace.ToWorld(c.Position);
                d.Radius = c.Radius;
                d.Remaining = c.Remaining;
                d.Ignited = c.IsIgnited;
                CagesData.Set(i, d);
            }
        }

        private void ReadState()
        {
            ReadTicks++;

            int playerCount = _sim.Players.Count;
            for (int i = 0; i < playerCount && i < MaxPlayers; i++)
            {
                PlayerNetData d = PlayersData[i];
                PlayerState p = _sim.Players[i];
                p.Position = ArenaSpace.ToPlanar(d.Position);
                p.Facing = ArenaSpace.ToPlanar(d.Facing);
                p.Health.Current = d.Health;
                p.Health.Max = d.MaxHealth;
                p.Health.IsDefeated = d.Defeated;
                p.AimTarget = ArenaSpace.ToPlanar(d.AimTarget);
                p.HasAimTarget = d.HasAimTarget;
                p.AimTargetClamped = d.AimTargetClamped;
            }

            int enemyCount = EnemyCount;
            if (enemyCount > _sim.Enemies.Count) enemyCount = _sim.Enemies.Count;
            for (int i = 0; i < enemyCount && i < MaxEnemies; i++)
            {
                EnemyNetData d = EnemiesData[i];
                EnemyState e = _sim.Enemies[i];
                e.Position = ArenaSpace.ToPlanar(d.Position);
                e.Health.Current = d.Health;
                e.Health.IsDefeated = d.Defeated;
            }

            int cageCount = CageCount;
            if (cageCount > 4) cageCount = 4;
            if (cageCount < 0) cageCount = 0;
            CageSnapshot[] cages = new CageSnapshot[cageCount];
            for (int i = 0; i < cageCount; i++)
            {
                CageNetData d = CagesData[i];
                CageSnapshot s = default(CageSnapshot);
                s.Id = i;
                s.Position = ArenaSpace.ToPlanar(d.Position);
                s.Radius = d.Radius;
                s.Remaining = d.Remaining;
                s.Ignited = d.Ignited;
                s.IgnitedTimeRemaining = d.Ignited ? d.Remaining : 0f;
                cages[i] = s;
            }
            _sim.ApplyCages(cages);
        }

        private void BuildRefs()
        {
            _refs.Clear();
            foreach (PlayerRef p in Runner.ActivePlayers) _refs.Add(p);
            _refs.Sort((a, b) => a.AsIndex.CompareTo(b.AsIndex));
        }

        private int LocalIndex()
        {
            BuildRefs();
            for (int i = 0; i < _refs.Count; i++)
                if (_refs[i] == Runner.LocalPlayer) return i;
            return 0;
        }

        private static PlayerCommand ToCommand(RootboundInput input)
        {
            PlayerCommand cmd = default(PlayerCommand);
            cmd.Move = new Vec2(input.Move.x, input.Move.y);
            cmd.Aim = new Vec2(input.Aim.x, input.Aim.y);
            cmd.TargetPoint = new Vec2(input.TargetPoint.x, input.TargetPoint.y);
            cmd.HasTargetPoint = input.HasTargetPoint;
            cmd.Primary = input.Primary;
            cmd.Special = input.Special;
            cmd.Dodge = input.Dodge;
            cmd.Interact = input.Interact;
            return cmd;
        }
    }
}
