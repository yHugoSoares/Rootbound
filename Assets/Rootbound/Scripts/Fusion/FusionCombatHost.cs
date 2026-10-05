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

    // Host-authoritative match. The host owns the single CombatSimulation and
    // replicates player state, enemies and cages; clients apply it to a local
    // mirror. The lobby starts with only the connected players (no enemies) and
    // the host starts the encounter with R. Each player picks their own creature.
    public sealed class FusionCombatHost : NetworkBehaviour
    {
        private const int MaxPlayers = 2;
        private const int MaxEnemies = 8;
        private const int EncounterEnemies = 8;

        public static FusionCombatHost LocalInstance;

        [Networked] private int PlayerCountNet { get; set; }

        [Networked, Capacity(MaxPlayers)]
        private NetworkArray<byte> PlayerCreatures => default;

        [Networked] private NetworkBool EncounterStarted { get; set; }

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
        public Vector3 NetPlayer0Position { get { return PlayersData[0].Position; } }

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
        private bool _builtStarted;
        private bool _restartRequested;

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

            bool finished = _sim.EncounterCleared || _sim.AllPlayersDefeated;
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
                _sim.SubmitCommand(i, cmd);
            }

            if ((!EncounterStarted || finished) && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                _restartRequested = true;

            if (_restartRequested)
            {
                _restartRequested = false;
                EncounterStarted = true;
                EnsureSimulation();
            }

            _sim.Step();
            WriteState();
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
            if (_view != null) _view.Render(_sim, Time.deltaTime);
            if (_hud != null)
            {
                _hud.Paused = false;
                _hud.LocalPlayerId = LocalIndex();
                _hud.WaitingForStart = !EncounterStarted;
                _hud.WaitingLabel = HasStateAuthority
                    ? "Waiting for players - press R to start"
                    : "Waiting for host to start";
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
            int desiredCount = PlayerCountNet;
            if (desiredCount < 1) desiredCount = 1;
            if (desiredCount > MaxPlayers) desiredCount = 2;

            bool started = EncounterStarted;
            int enemies = started ? EncounterEnemies : 0;

            bool creaturesChanged = false;
            for (int i = 0; i < desiredCount; i++)
                if (_builtCreatures[i] != PlayerCreatures[i]) creaturesChanged = true;

            bool changed = _sim == null
                || _builtPlayerCount != desiredCount
                || _builtStarted != started
                || _builtEnemyCount != enemies
                || creaturesChanged;
            if (!changed) return;

            CreatureKind[] kinds = new CreatureKind[desiredCount];
            for (int i = 0; i < desiredCount; i++)
            {
                kinds[i] = (CreatureKind)PlayerCreatures[i];
                _builtCreatures[i] = PlayerCreatures[i];
            }

            _sim = new CombatSimulation(CombatSetup.Coop(kinds, enemies));
            _builtPlayerCount = desiredCount;
            _builtStarted = started;
            _builtEnemyCount = enemies;
            RebindView();
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
            return cmd;
        }
    }
}
