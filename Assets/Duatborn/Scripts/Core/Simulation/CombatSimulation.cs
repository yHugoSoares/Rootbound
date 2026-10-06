using System;
using System.Collections.Generic;

namespace Duatborn.Core
{
    public sealed class CombatSimulation
    {
        private readonly CombatSetup _setup;
        private readonly SimConfig _config;
        private readonly List<PlayerState> _players = new List<PlayerState>();
        private readonly List<EnemyState> _enemies = new List<EnemyState>();
        private readonly List<ProjectileState> _projectiles = new List<ProjectileState>();
        private readonly List<BindingSealState> _cages = new List<BindingSealState>();
        private readonly List<SimEvent> _events = new List<SimEvent>();
        private readonly Dictionary<int, PlayerCommand> _commands = new Dictionary<int, PlayerCommand>();

        private int _nextEnemyId;
        private int _nextProjectileId;
        private int _nextCageId;
        private bool _encounterCleared;
        private bool _allPlayersDefeated;

        public int Tick { get; private set; }
        public float ElapsedTime { get; private set; }
        public float DeltaTime { get; private set; }
        public SimConfig Config { get { return _config; } }
        public IReadOnlyList<PlayerState> Players { get { return _players; } }
        public IReadOnlyList<EnemyState> Enemies { get { return _enemies; } }
        public IReadOnlyList<ProjectileState> Projectiles { get { return _projectiles; } }
        public IReadOnlyList<BindingSealState> Cages { get { return _cages; } }
        public IReadOnlyList<SimEvent> Events { get { return _events; } }
        public bool EncounterCleared { get { return _encounterCleared; } }
        public bool AllPlayersDefeated { get { return _allPlayersDefeated; } }
        public CombatSetup Setup { get { return _setup; } }

        public CombatSimulation(CombatSetup setup)
        {
            if (setup == null) throw new ArgumentNullException("setup");
            if (setup.PlayerSpecs == null || setup.PlayerSpecs.Length == 0)
                throw new ArgumentException("CombatSetup requires at least one player spec.", "setup");
            if (setup.EnemySpec == null)
                throw new ArgumentException("CombatSetup requires an enemy spec.", "setup");
            _setup = setup;
            _config = setup.SimConfig;
            DeltaTime = 1f / _config.TickRate;
            Rebuild();
        }

        public void Reset()
        {
            Rebuild();
        }

        public void SubmitCommand(int playerId, PlayerCommand command)
        {
            _commands[playerId] = command;
        }

        public PlayerState GetPlayer(int id)
        {
            for (int i = 0; i < _players.Count; i++)
                if (_players[i].Id == id) return _players[i];
            return null;
        }

        public EnemyState GetEnemy(int id)
        {
            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i].Id == id) return _enemies[i];
            return null;
        }

        public BindingSealState GetCage(int id)
        {
            for (int i = 0; i < _cages.Count; i++)
                if (_cages[i].Id == id) return _cages[i];
            return null;
        }

        public PlayerSnapshot[] CapturePlayers()
        {
            PlayerSnapshot[] result = new PlayerSnapshot[_players.Count];
            for (int i = 0; i < _players.Count; i++)
            {
                PlayerState p = _players[i];
                PlayerSnapshot s = default(PlayerSnapshot);
                s.Id = p.Id;
                s.Position = p.Position;
                s.Facing = p.Facing;
                s.Health = p.Health.Current;
                s.MaxHealth = p.Health.Max;
                s.Defeated = p.Health.IsDefeated;
                s.PrimaryCooldown = p.PrimaryCooldown.Remaining;
                s.SpecialCooldown = p.SpecialCooldown.Remaining;
                s.DodgeCooldown = p.DodgeCooldown.Remaining;
                s.DodgeDuration = p.Dodge.Duration;
                s.DodgeActive = p.Dodge.IsActive;
                s.DodgeDirection = p.Dodge.Direction;
                result[i] = s;
            }
            return result;
        }

        public CageSnapshot[] CaptureCages()
        {
            CageSnapshot[] result = new CageSnapshot[_cages.Count];
            for (int i = 0; i < _cages.Count; i++)
            {
                BindingSealState c = _cages[i];
                CageSnapshot s = default(CageSnapshot);
                s.Id = c.Id;
                s.Position = c.Position;
                s.Radius = c.Radius;
                s.Remaining = c.Remaining;
                s.Ignited = c.IsIgnited;
                s.IgnitedTimeRemaining = c.IgnitedTimeRemaining;
                result[i] = s;
            }
            return result;
        }

        public void ApplyCages(CageSnapshot[] cages)
        {
            _cages.Clear();
            if (cages == null) return;
            for (int i = 0; i < cages.Length; i++)
            {
                CageSnapshot s = cages[i];
                BindingSealState c = new BindingSealState();
                c.Id = s.Id;
                c.Position = s.Position;
                c.Radius = s.Radius;
                c.Duration = s.Remaining;
                c.Elapsed = 0f;
                c.IsIgnited = s.Ignited;
                c.IgnitedTimeRemaining = s.Ignited ? s.Remaining : 0f;
                c.MaxIgnitionDuration = c.IgnitedTimeRemaining;
                _cages.Add(c);
            }
        }

        public void ApplyPlayerSnapshots(PlayerSnapshot[] snapshots)
        {
            if (snapshots == null) return;
            int count = snapshots.Length < _players.Count ? snapshots.Length : _players.Count;
            for (int i = 0; i < count; i++)
            {
                PlayerSnapshot s = snapshots[i];
                PlayerState p = _players[i];
                p.Position = s.Position;
                p.Facing = s.Facing;
                p.Health.Current = s.Health;
                p.Health.Max = s.MaxHealth;
                p.Health.IsDefeated = s.Defeated;
                p.PrimaryCooldown.Remaining = s.PrimaryCooldown;
                p.SpecialCooldown.Remaining = s.SpecialCooldown;
                p.DodgeCooldown.Remaining = s.DodgeCooldown;
                p.Dodge.IsActive = s.DodgeActive;
                p.Dodge.Duration = s.DodgeDuration;
                p.Dodge.Direction = s.DodgeDirection;
            }
        }

        public void Step()
        {
            Step(DeltaTime);
        }

        public void Step(float dt)
        {
            if (dt <= 0f) return;
            _events.Clear();
            UpdateCooldowns(dt);
            ElapsedTime += dt;
            ApplyPlayerCommands(dt);
            UpdatePlayerMovement(dt);
            UpdateDodges(dt);
            UpdateAbilities(dt);
            UpdateCages(dt);
            UpdateEnemies(dt);
            UpdateProjectiles(dt);
            ClampEntities();
            EvaluateEndStates();
            Tick++;
        }

        private void UpdateCooldowns(float dt)
        {
            for (int i = 0; i < _players.Count; i++)
            {
                PlayerState p = _players[i];
                p.PrimaryCooldown.Tick(dt);
                p.SpecialCooldown.Tick(dt);
                p.DodgeCooldown.Tick(dt);
            }
            for (int i = 0; i < _enemies.Count; i++)
            {
                _enemies[i].AttackCooldown.Tick(dt);
            }
        }

        private void Rebuild()
        {
            _players.Clear();
            _enemies.Clear();
            _projectiles.Clear();
            _cages.Clear();
            _events.Clear();
            _commands.Clear();
            _nextEnemyId = 1;
            _nextProjectileId = 1;
            _nextCageId = 1;
            _encounterCleared = false;
            _allPlayersDefeated = false;
            Tick = 0;
            ElapsedTime = 0f;

            CreatureSpec[] specs = _setup.PlayerSpecs;
            for (int i = 0; i < specs.Length; i++)
            {
                Vec2 spawn = HasSpawn(_setup.PlayerSpawns, i)
                    ? _setup.PlayerSpawns[i]
                    : DefaultPlayerSpawn(i, specs.Length);
                PlayerState p = new PlayerState();
                p.Id = i;
                p.Spec = specs[i];
                p.Position = spawn;
                p.Facing = new Vec2(0f, 1f);
                p.Health = Health.Create(specs[i].MaxHealth);
                _players.Add(p);
            }

            int count = _setup.EnemyCount;
            for (int i = 0; i < count; i++)
            {
                Vec2 spawn = HasSpawn(_setup.EnemySpawns, i)
                    ? _setup.EnemySpawns[i]
                    : DefaultEnemySpawn(i, count);
                EnemySpec spec = _setup.EnemySpecs != null && i < _setup.EnemySpecs.Length && _setup.EnemySpecs[i] != null
                    ? _setup.EnemySpecs[i]
                    : _setup.EnemySpec;
                EnemyState e = new EnemyState();
                e.Id = _nextEnemyId++;
                e.Spec = spec;
                e.Position = spawn;
                e.Facing = new Vec2(0f, -1f);
                e.Health = Health.Create(spec.MaxHealth);
                _enemies.Add(e);
            }
        }

        private static bool HasSpawn(Vec2[] spawns, int index)
        {
            return spawns != null && index < spawns.Length;
        }

        private static Vec2 DefaultPlayerSpawn(int index, int total)
        {
            float angle = total <= 1 ? 90f : 90f + index * (360f / total);
            float rad = angle * DefaultContent.DegreesToRadians;
            return new Vec2((float)Math.Cos(rad) * 3f, (float)Math.Sin(rad) * 3f);
        }

        private static Vec2 DefaultEnemySpawn(int index, int total)
        {
            float angle = (total <= 1 ? 0f : index * (360f / total)) + 30f;
            float rad = angle * DefaultContent.DegreesToRadians;
            return new Vec2((float)Math.Cos(rad) * 12f, (float)Math.Sin(rad) * 12f);
        }

        private void ApplyPlayerCommands(float dt)
        {
            for (int i = 0; i < _players.Count; i++)
            {
                PlayerState p = _players[i];
                if (p.Health.IsDefeated) continue;

                PlayerCommand cmd;
                if (!_commands.TryGetValue(p.Id, out cmd)) cmd = default(PlayerCommand);

                bool hasAim = cmd.Aim.SqrMagnitude > 0.0001f;
                bool hasMove = cmd.Move.SqrMagnitude > 0.0001f;
                if (hasAim) p.Facing = cmd.Aim.Normalized;
                else if (hasMove) p.Facing = cmd.Move.Normalized;

                p.MoveInput = Vec2.ClampMagnitude(cmd.Move, 1f);
                CreatureSpec spec = p.Spec;

                Vec2 requestedTarget = cmd.HasTargetPoint
                    ? cmd.TargetPoint
                    : p.Position + p.Facing * spec.Special.CastRange;
                bool clamped;
                Vec2 aimTarget = ClampCastTarget(p.Position, requestedTarget, spec.Special.CastRange, out clamped);
                p.AimTarget = aimTarget;
                p.HasAimTarget = true;
                p.AimTargetClamped = clamped;

                if (cmd.Dodge)
                {
                    if (p.Dodge.IsActive)
                    {
                        RecordRequest(p, AbilitySlot.Dodge, false, "already dodging");
                    }
                    else if (!p.DodgeCooldown.IsReady)
                    {
                        RecordRequest(p, AbilitySlot.Dodge, false, "cooldown " + CooldownDisplay.Seconds(p.DodgeCooldown.Remaining) + "s");
                    }
                    else
                    {
                        Vec2 dir = hasMove ? cmd.Move.Normalized : p.Facing;
                        p.Dodge.Begin(dir, spec.Dodge.Duration, spec.Dodge.InvulnStart, spec.Dodge.InvulnEnd);
                        p.DodgeCooldown.TryStart(spec.Dodge.Cooldown);
                        p.Primary.Cancel();
                        p.Special.Cancel();
                        Raise(SimEventKind.AbilityActivated, p.Id, -1, p.Position, 0f, (int)AbilitySlot.Dodge);
                        RecordRequest(p, AbilitySlot.Dodge, true, "accepted");
                    }
                }

                if (cmd.Primary)
                {
                    if (p.Dodge.IsActive)
                    {
                        RecordRequest(p, AbilitySlot.Primary, false, "dodging");
                    }
                    else if (p.Primary.Phase != AbilityPhase.Ready)
                    {
                        RecordRequest(p, AbilitySlot.Primary, false, "busy");
                    }
                    else if (!p.PrimaryCooldown.IsReady)
                    {
                        RecordRequest(p, AbilitySlot.Primary, false, "cooldown " + CooldownDisplay.Seconds(p.PrimaryCooldown.Remaining) + "s");
                    }
                    else
                    {
                        p.Primary.Begin(spec.Primary.Windup, spec.Primary.Active, spec.Primary.Recovery);
                        p.PrimaryCooldown.TryStart(spec.Primary.Cooldown);
                        RecordRequest(p, AbilitySlot.Primary, true, "accepted");
                    }
                }

                if (cmd.Special)
                {
                    if (p.Dodge.IsActive)
                    {
                        RecordRequest(p, AbilitySlot.Special, false, "dodging");
                    }
                    else if (p.Special.Phase != AbilityPhase.Ready)
                    {
                        RecordRequest(p, AbilitySlot.Special, false, "busy");
                    }
                    else if (!p.SpecialCooldown.IsReady)
                    {
                        RecordRequest(p, AbilitySlot.Special, false, "cooldown " + CooldownDisplay.Seconds(p.SpecialCooldown.Remaining) + "s");
                    }
                    else
                    {
                        p.Special.Begin(spec.Special.Windup, spec.Special.Active, spec.Special.Recovery);
                        p.SpecialCooldown.TryStart(spec.Special.Cooldown);
                        p.SpecialTarget = aimTarget;
                        p.HasSpecialTarget = true;
                        RecordRequest(p, AbilitySlot.Special, true, "accepted");
                    }
                }
            }
        }

        private static Vec2 ClampCastTarget(Vec2 from, Vec2 raw, float maxRange, out bool clamped)
        {
            Vec2 delta = raw - from;
            float distance = delta.Magnitude;
            if (distance <= maxRange || distance <= 1e-5f)
            {
                clamped = false;
                return raw;
            }
            clamped = true;
            return from + delta * (maxRange / distance);
        }

        private void RecordRequest(PlayerState p, AbilitySlot slot, bool accepted, string reason)
        {
            p.LastRequestedSlot = slot;
            p.LastRequestAccepted = accepted;
            p.LastRequestReason = reason;
            p.LastRequestTick = Tick;
        }

        private void UpdatePlayerMovement(float dt)
        {
            for (int i = 0; i < _players.Count; i++)
            {
                PlayerState p = _players[i];
                if (p.Health.IsDefeated) continue;
                if (p.Dodge.IsActive || p.Primary.IsMovementLocked || p.Special.IsMovementLocked) continue;
                p.Position = p.Position + p.MoveInput * (p.Spec.MoveSpeed * dt);
            }
        }

        private void UpdateDodges(float dt)
        {
            for (int i = 0; i < _players.Count; i++)
            {
                PlayerState p = _players[i];
                if (p.Health.IsDefeated) continue;
                if (!p.Dodge.IsActive) continue;
                p.Position = p.Position + p.Dodge.Tick(dt, p.Spec.Dodge.Distance);
            }
        }

        private void UpdateAbilities(float dt)
        {
            for (int i = 0; i < _players.Count; i++)
            {
                PlayerState p = _players[i];
                if (p.Health.IsDefeated) continue;
                if (p.Primary.Tick(dt)) ResolvePrimary(p);
                if (p.Special.Tick(dt)) ResolveSpecial(p);
            }
        }

        private void ResolvePrimary(PlayerState p)
        {
            AttackSpec a = p.Spec.Primary;
            Raise(SimEventKind.AbilityActivated, p.Id, -1, p.Position, a.Damage, (int)AbilitySlot.Primary);

            if (a.Kind == AttackKind.MeleeSweep)
            {
                for (int i = 0; i < _enemies.Count; i++)
                {
                    EnemyState e = _enemies[i];
                    if (e.Health.IsDefeated) continue;
                    Vec2 to = e.Position - p.Position;
                    if (to.Magnitude > a.Range + e.Spec.BodyRadius) continue;
                    if (!TargetRules.WithinArc(p.Facing, to, a.ArcDegrees)) continue;
                    ApplyDamageToEnemy(e, a.Damage, p.Id);
                }
            }
            else
            {
                Vec2 dir = p.Facing.SqrMagnitude > 0.0001f ? p.Facing.Normalized : new Vec2(0f, 1f);
                ProjectileState proj = new ProjectileState();
                proj.Id = _nextProjectileId++;
                proj.OwnerPlayerId = p.Id;
                proj.Position = p.Position + dir * (p.Spec.BodyRadius + 0.1f);
                proj.Velocity = dir * a.ProjectileSpeed;
                proj.Damage = a.Damage;
                proj.Radius = a.ProjectileRadius;
                proj.RemainingLife = a.ProjectileLifetime;
                proj.IsAlive = true;
                _projectiles.Add(proj);
                Raise(SimEventKind.ProjectileSpawned, p.Id, proj.Id, proj.Position, 0f, proj.Id);
            }
        }

        private void ResolveSpecial(PlayerState p)
        {
            SpecialSpec s = p.Spec.Special;
            Vec2 dir = p.Facing.SqrMagnitude > 0.0001f ? p.Facing.Normalized : new Vec2(0f, 1f);
            Vec2 target = p.HasSpecialTarget ? p.SpecialTarget : p.Position + dir * s.CastRange;
            Raise(SimEventKind.AbilityActivated, p.Id, -1, p.Position, s.Damage, (int)AbilitySlot.Special);

            if (s.Kind == SpecialKind.BindingSeal)
            {
                BindingSealState cage = new BindingSealState();
                cage.Id = _nextCageId++;
                cage.OwnerPlayerId = p.Id;
                cage.IgnitedByPlayerId = -1;
                cage.Position = target;
                cage.Radius = s.Radius;
                cage.Duration = s.Duration;
                cage.Elapsed = 0f;
                cage.RestrainFactor = s.RestrainFactor;
                cage.FireTickInterval = s.FireTickInterval;
                cage.FireDamagePerTick = s.FireDamagePerTick;
                cage.MaxIgnitionDuration = s.MaxIgnitionDuration;
                cage.IgnitedTimeRemaining = 0f;
                cage.FireTickAccumulator = 0f;
                cage.IsIgnited = false;
                _cages.Add(cage);
                Raise(SimEventKind.CageCreated, p.Id, cage.Id, cage.Position, s.Duration, cage.Id);
            }
            else
            {
                if (s.Damage > 0f)
                {
                    for (int i = 0; i < _enemies.Count; i++)
                    {
                        EnemyState e = _enemies[i];
                        if (e.Health.IsDefeated) continue;
                        if (Vec2.Distance(e.Position, target) > s.Radius + e.Spec.BodyRadius) continue;
                        ApplyDamageToEnemy(e, s.Damage, p.Id);
                    }
                }

                for (int i = 0; i < _cages.Count; i++)
                {
                    BindingSealState cage = _cages[i];
                    if (!cage.IsActive) continue;
                    if (Vec2.Distance(cage.Position, target) > s.Radius + cage.Radius) continue;
                    bool newlyIgnited = cage.TryIgnite(s.IgnitionExtension);
                    if (!newlyIgnited) continue;
                    cage.IgnitedByPlayerId = p.Id;
                    Raise(SimEventKind.CageIgnited, p.Id, cage.Id, cage.Position, s.IgnitionExtension, cage.Id);
                }
            }
        }

        private void UpdateCages(float dt)
        {
            for (int i = _cages.Count - 1; i >= 0; i--)
            {
                BindingSealState cage = _cages[i];
                cage.Tick(dt);

                if (cage.ConsumeFireTick())
                {
                    for (int j = 0; j < _enemies.Count; j++)
                    {
                        EnemyState e = _enemies[j];
                        if (e.Health.IsDefeated) continue;
                        if (Vec2.Distance(e.Position, cage.Position) > cage.Radius + e.Spec.BodyRadius) continue;
                        ApplyDamageToEnemy(e, cage.FireDamagePerTick, cage.IgnitedByPlayerId);
                    }
                    Raise(SimEventKind.FireTick, cage.IgnitedByPlayerId, -1, cage.Position, cage.FireDamagePerTick, cage.Id);
                }

                if (!cage.IsActive)
                {
                    Raise(SimEventKind.CageExpired, cage.OwnerPlayerId, -1, cage.Position, 0f, cage.Id);
                    _cages.RemoveAt(i);
                }
            }
        }

        private void UpdateEnemies(float dt)
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                EnemyState e = _enemies[i];
                if (e.Health.IsDefeated) continue;
                EnemySpec spec = e.Spec;

                e.RestrainAmount = GetRestrainAmount(e.Position);
                e.IsRestrained = e.RestrainAmount >= 0.999f;

                if (e.Attack.Tick(dt)) ResolveEnemyAttack(e);

                PlayerState target = FindNearestLivingPlayer(e.Position);
                if (target == null) continue;

                Vec2 to = target.Position - e.Position;
                float dist = to.Magnitude;
                if (dist > 0.0001f) e.Facing = to.Normalized;

                if (e.Attack.IsBusy) continue;

                if (dist <= spec.AttackRange)
                {
                    if (!e.IsRestrained && e.AttackCooldown.IsReady && e.Attack.Phase == AbilityPhase.Ready)
                    {
                        e.Attack.Begin(spec.AttackWindup, spec.AttackActive, spec.AttackRecovery);
                        e.AttackCooldown.TryStart(spec.AttackCooldown);
                    }
                }
                else
                {
                    float speed = spec.MoveSpeed * (1f - e.RestrainAmount);
                    e.Position = e.Position + e.Facing * (speed * dt);
                }
            }
        }

        private void ResolveEnemyAttack(EnemyState e)
        {
            EnemySpec spec = e.Spec;
            Raise(SimEventKind.AbilityActivated, e.Id, -1, e.Position, spec.AttackDamage, (int)AbilitySlot.Primary);
            for (int i = 0; i < _players.Count; i++)
            {
                PlayerState p = _players[i];
                if (p.Health.IsDefeated) continue;
                Vec2 to = p.Position - e.Position;
                if (to.Magnitude > spec.AttackRange + p.Spec.BodyRadius) continue;
                if (!TargetRules.WithinArc(e.Facing, to, spec.AttackArcDegrees)) continue;
                ApplyDamageToPlayer(p, spec.AttackDamage, e.Id);
            }
        }

        private void UpdateProjectiles(float dt)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                ProjectileState proj = _projectiles[i];
                if (!proj.IsAlive)
                {
                    _projectiles.RemoveAt(i);
                    continue;
                }

                proj.RemainingLife -= dt;
                proj.Position = proj.Position + proj.Velocity * dt;

                bool remove = proj.RemainingLife <= 0f
                    || proj.Position.SqrMagnitude > _config.ArenaRadius * _config.ArenaRadius;

                if (!remove)
                {
                    for (int j = 0; j < _enemies.Count; j++)
                    {
                        EnemyState e = _enemies[j];
                        if (e.Health.IsDefeated) continue;
                        float rr = proj.Radius + e.Spec.BodyRadius;
                        if (Vec2.SqrDistance(proj.Position, e.Position) > rr * rr) continue;
                        ApplyDamageToEnemy(e, proj.Damage, proj.OwnerPlayerId);
                        remove = true;
                        break;
                    }
                }

                if (remove) _projectiles.RemoveAt(i);
            }
        }

        private void ClampEntities()
        {
            float r = _config.ArenaRadius;
            for (int i = 0; i < _players.Count; i++)
                _players[i].Position = Vec2.ClampMagnitude(_players[i].Position, r);
            for (int i = 0; i < _enemies.Count; i++)
                _enemies[i].Position = Vec2.ClampMagnitude(_enemies[i].Position, r);
        }

        private void EvaluateEndStates()
        {
            bool allEnemiesDown = _enemies.Count > 0;
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (!_enemies[i].Health.IsDefeated) { allEnemiesDown = false; break; }
            }
            if (allEnemiesDown && !_encounterCleared)
            {
                _encounterCleared = true;
                Raise(SimEventKind.EncounterCleared, -1, -1, Vec2.Zero, 0f, 0);
            }

            bool allPlayersDown = _players.Count > 0;
            for (int i = 0; i < _players.Count; i++)
            {
                if (!_players[i].Health.IsDefeated) { allPlayersDown = false; break; }
            }
            if (allPlayersDown && !_allPlayersDefeated)
            {
                _allPlayersDefeated = true;
                Raise(SimEventKind.AllPlayersDefeated, -1, -1, Vec2.Zero, 0f, 0);
            }
        }

        private float GetRestrainAmount(Vec2 position)
        {
            float amount = 0f;
            for (int i = 0; i < _cages.Count; i++)
            {
                BindingSealState cage = _cages[i];
                if (!cage.IsActive) continue;
                if (Vec2.Distance(position, cage.Position) > cage.Radius) continue;
                if (cage.RestrainFactor > amount) amount = cage.RestrainFactor;
            }
            return amount;
        }

        private PlayerState FindNearestLivingPlayer(Vec2 from)
        {
            PlayerState best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < _players.Count; i++)
            {
                PlayerState p = _players[i];
                if (p.Health.IsDefeated) continue;
                float d = Vec2.SqrDistance(from, p.Position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = p;
                }
            }
            return best;
        }

        private void ApplyDamageToPlayer(PlayerState p, float amount, int sourceId)
        {
            if (p.Health.IsDefeated) return;
            if (p.Dodge.IsInvulnerable) return;
            float applied = p.Health.ApplyDamage(amount);
            if (applied > 0f)
                Raise(SimEventKind.DamageDealt, sourceId, p.Id, p.Position, applied, 0);
            if (p.Health.IsDefeated)
                Raise(SimEventKind.EntityDefeated, sourceId, p.Id, p.Position, 0f, 0);
        }

        private void ApplyDamageToEnemy(EnemyState e, float amount, int sourceId)
        {
            if (e.Health.IsDefeated) return;
            float applied = e.Health.ApplyDamage(amount);
            if (applied > 0f)
                Raise(SimEventKind.DamageDealt, sourceId, e.Id, e.Position, applied, 0);
            if (e.Health.IsDefeated)
            {
                Raise(SimEventKind.EntityDefeated, sourceId, e.Id, e.Position, 0f, 0);
                ApplyLifeOnKill(sourceId);
                ApplyDeathBurst(e);
            }
        }

        private void ApplyLifeOnKill(int sourceId)
        {
            PlayerState killer = GetPlayer(sourceId);
            if (killer == null || killer.Spec == null || killer.Health.IsDefeated) return;
            if (killer.Spec.LifeOnKill <= 0f) return;
            killer.Health.Heal(killer.Spec.LifeOnKill);
        }

        private void ApplyDeathBurst(EnemyState e)
        {
            EnemySpec spec = e.Spec;
            if (spec == null || spec.DeathBurstRadius <= 0f || spec.DeathBurstDamage <= 0f) return;
            float sqr = spec.DeathBurstRadius * spec.DeathBurstRadius;
            for (int i = 0; i < _players.Count; i++)
            {
                PlayerState p = _players[i];
                if (p.Health.IsDefeated) continue;
                if (Vec2.SqrDistance(p.Position, e.Position) > sqr) continue;
                ApplyDamageToPlayer(p, spec.DeathBurstDamage, e.Id);
            }
        }

        private void Raise(SimEventKind kind, int sourceId, int targetId, Vec2 position, float amount, int extraId)
        {
            SimEvent e = new SimEvent();
            e.Kind = kind;
            e.SourceId = sourceId;
            e.TargetId = targetId;
            e.Position = position;
            e.Amount = amount;
            e.ExtraId = extraId;
            _events.Add(e);
        }
    }
}
