using System.Collections.Generic;
using UnityEngine;
using Rootbound.Core;

namespace Rootbound.Unity
{
    public sealed class CombatView : MonoBehaviour
    {
        private static readonly Color RootColor = new Color(0.76f, 0.63f, 0.35f);
        private static readonly Color MothColor = new Color(0.91f, 0.64f, 0.24f);
        private static readonly Color EnemyColor = new Color(0.42f, 0.43f, 0.46f);
        private static readonly Color RestrainedColor = new Color(0.25f, 0.35f, 0.6f);
        private static readonly Color CageColor = new Color(0.18f, 0.37f, 0.64f);
        private static readonly Color CageIgnitedColor = new Color(0.95f, 0.71f, 0.25f);
        private static readonly Color ProjectileColor = new Color(1f, 0.82f, 0.4f);
        private static readonly Color TargetColor = new Color(0.4f, 0.9f, 1f);
        private static readonly Color TargetClampedColor = new Color(1f, 0.6f, 0.2f);

        private readonly Dictionary<int, Transform> _aimMarkers = new Dictionary<int, Transform>();
        private readonly Dictionary<int, Renderer> _aimMarkerRenderers = new Dictionary<int, Renderer>();
        private readonly Dictionary<int, Transform> _pickupViews = new Dictionary<int, Transform>();
        private readonly Dictionary<int, Renderer> _pickupRenderers = new Dictionary<int, Renderer>();
        private readonly Dictionary<int, TextMesh> _pickupLabels = new Dictionary<int, TextMesh>();
        private IReadOnlyList<UpgradePickupState> _pickups;
        private readonly HashSet<int> _defeatedEnemies = new HashSet<int>();
        private readonly List<DeathBurstView> _bursts = new List<DeathBurstView>();

        private sealed class DeathBurstView
        {
            public GameObject Go;
            public float Elapsed;
            public float Duration;
            public float Radius;
        }

        public int LocalPlayerId = -1;
        public float PickupLabelRange = 3f;

        public void SetPickups(IReadOnlyList<UpgradePickupState> pickups)
        {
            _pickups = pickups;
        }
        private readonly Dictionary<int, Transform> _players = new Dictionary<int, Transform>();
        private readonly Dictionary<int, Transform> _enemies = new Dictionary<int, Transform>();
        private readonly Dictionary<int, Transform> _cages = new Dictionary<int, Transform>();
        private readonly Dictionary<int, Transform> _projectiles = new Dictionary<int, Transform>();
        private readonly Dictionary<int, Renderer> _enemyRenderers = new Dictionary<int, Renderer>();
        private readonly Dictionary<int, Renderer> _cageRenderers = new Dictionary<int, Renderer>();
        private readonly Dictionary<int, float> _hitTimers = new Dictionary<int, float>();
        private readonly HashSet<int> _seen = new HashSet<int>();
        private readonly List<int> _stale = new List<int>();

        public void Bind(CombatSimulation sim)
        {
            ClearChildren();
            _defeatedEnemies.Clear();
            _bursts.Clear();
            EnsureDressing();
            _players.Clear();
            _enemies.Clear();
            _cages.Clear();
            _projectiles.Clear();
            _enemyRenderers.Clear();
            _cageRenderers.Clear();
            _hitTimers.Clear();
            _aimMarkers.Clear();
            _aimMarkerRenderers.Clear();

            bool development = Application.isEditor || Debug.isDebugBuild;
            for (int i = 0; i < sim.Players.Count; i++)
            {
                PlayerState p = sim.Players[i];
                Color color = p.Spec.Kind == CreatureKind.RootGuardian ? RootColor : MothColor;
                Transform t = PlaceholderVisuals.CreateCapsule("Player_" + p.Id, color).transform;
                t.SetParent(transform, false);
                float radius = p.Spec.BodyRadius * 2f;
                t.localScale = new Vector3(radius, 0.9f, radius);
                _players[p.Id] = t;

                if (development)
                {
                    GameObject marker = PlaceholderVisuals.CreateCylinder("AimMarker_" + p.Id, TargetColor);
                    marker.transform.SetParent(transform, false);
                    marker.transform.localScale = new Vector3(0.6f, 0.01f, 0.6f);
                    _aimMarkers[p.Id] = marker.transform;
                    _aimMarkerRenderers[p.Id] = marker.GetComponent<Renderer>();
                }
            }

            for (int i = 0; i < sim.Enemies.Count; i++)
            {
                EnemyState e = sim.Enemies[i];
                GameObject go = PlaceholderVisuals.CreateSphere("Enemy_" + e.Id, EnemyColor);
                go.transform.SetParent(transform, false);
                go.transform.localScale = Vector3.one * (e.Spec.BodyRadius * 2f);
                _enemies[e.Id] = go.transform;
                _enemyRenderers[e.Id] = go.GetComponent<Renderer>();
            }
        }

        public void Render(CombatSimulation sim, float dt)
        {
            RenderPlayers(sim);
            RenderEnemies(sim, dt);
            UpdateBursts(dt);
            RenderAimMarkers(sim);
            SyncCages(sim);
            SyncProjectiles(sim);
            SyncPickups(sim);
        }

        private void SyncPickups(CombatSimulation sim)
        {
            if (_pickups == null) return;

            _seen.Clear();
            for (int i = 0; i < _pickups.Count; i++)
            {
                UpgradePickupState p = _pickups[i];
                _seen.Add(p.Id);

                Transform t;
                if (!_pickupViews.TryGetValue(p.Id, out t))
                {
                    GameObject go = PlaceholderVisuals.CreateCylinder("Pickup_" + p.Id, PickupColor(p.UpgradeIndex));
                    go.transform.SetParent(transform, false);
                    t = go.transform;
                    _pickupViews[p.Id] = t;
                    _pickupRenderers[p.Id] = go.GetComponent<Renderer>();

                    GameObject labelGo = new GameObject("Label");
                    labelGo.transform.SetParent(t, false);
                    labelGo.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                    labelGo.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
                    TextMesh textMesh = labelGo.AddComponent<TextMesh>();
                    textMesh.text = PickupDescription(p.UpgradeIndex);
                    textMesh.characterSize = 0.12f;
                    textMesh.fontSize = 64;
                    textMesh.anchor = TextAnchor.MiddleCenter;
                    textMesh.alignment = TextAlignment.Center;
                    textMesh.color = Color.white;
                    Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (font != null)
                    {
                        textMesh.font = font;
                        Renderer labelRenderer = labelGo.GetComponent<MeshRenderer>();
                        if (labelRenderer != null) labelRenderer.sharedMaterial = font.material;
                    }
                    _pickupLabels[p.Id] = textMesh;
                }

                bool show = !p.Collected;
                t.gameObject.SetActive(show);
                if (!show) continue;
                t.position = ArenaSpace.ToWorld(p.Position) + Vector3.up * 0.5f;
                t.localScale = new Vector3(0.7f, 0.5f, 0.7f);
                PlaceholderVisuals.SetColor(_pickupRenderers[p.Id], PickupColor(p.UpgradeIndex));

                bool close = IsLocalPlayerClose(sim, p.Position);
                TextMesh label;
                if (_pickupLabels.TryGetValue(p.Id, out label) && label != null)
                    label.gameObject.SetActive(close);
            }

            RemoveStale(_pickupViews, _pickupRenderers);
            _stale.Clear();
            foreach (KeyValuePair<int, TextMesh> pair in _pickupLabels)
                if (!_seen.Contains(pair.Key)) _stale.Add(pair.Key);
            for (int i = 0; i < _stale.Count; i++) _pickupLabels.Remove(_stale[i]);
        }

        private bool IsLocalPlayerClose(CombatSimulation sim, Vec2 position)
        {
            if (LocalPlayerId >= 0)
            {
                PlayerState local = sim.GetPlayer(LocalPlayerId);
                return local != null && !local.IsDefeated
                    && Vec2.Distance(local.Position, position) <= PickupLabelRange;
            }

            for (int i = 0; i < sim.Players.Count; i++)
            {
                PlayerState p = sim.Players[i];
                if (p.IsDefeated) continue;
                if (Vec2.Distance(p.Position, position) <= PickupLabelRange) return true;
            }
            return false;
        }

        private static string PickupDescription(int upgradeIndex)
        {
            UpgradeSpec[] catalog = DefaultContent.UpgradeCatalog();
            if (upgradeIndex < 0 || upgradeIndex >= catalog.Length) return string.Empty;
            return catalog[upgradeIndex].DisplayName;
        }

        private void EnsureDressing()
        {
            GameObject root = new GameObject("GateDressing");
            root.transform.SetParent(transform, false);

            Color sandstone = new Color(0.72f, 0.62f, 0.4f);
            Color inscription = new Color(0.55f, 0.5f, 0.35f);

            GameObject leftPillar = PlaceholderVisuals.CreateCylinder("GateLeft", sandstone);
            leftPillar.transform.SetParent(root.transform, false);
            leftPillar.transform.position = new Vector3(-4f, 3f, 8f);
            leftPillar.transform.localScale = new Vector3(0.9f, 3f, 0.9f);

            GameObject rightPillar = PlaceholderVisuals.CreateCylinder("GateRight", sandstone);
            rightPillar.transform.SetParent(root.transform, false);
            rightPillar.transform.position = new Vector3(4f, 3f, 8f);
            rightPillar.transform.localScale = new Vector3(0.9f, 3f, 0.9f);

            GameObject lintel = PlaceholderVisuals.CreateCylinder("GateLintel", sandstone);
            lintel.transform.SetParent(root.transform, false);
            lintel.transform.position = new Vector3(0f, 6.1f, 8f);
            lintel.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            lintel.transform.localScale = new Vector3(0.7f, 4.5f, 0.7f);

            GameObject floorRing = PlaceholderVisuals.CreateCylinder("FloorInscription", inscription);
            floorRing.transform.SetParent(root.transform, false);
            floorRing.transform.position = new Vector3(0f, 0.02f, 0f);
            floorRing.transform.localScale = new Vector3(12f, 0.02f, 12f);
        }

        private void SpawnDeathBurst(Vec2 position, float radius)
        {
            GameObject go = PlaceholderVisuals.CreateCylinder("Burst_" + (_bursts.Count + 1), new Color(0.31f, 0.76f, 0.72f));
            go.transform.SetParent(transform, false);
            go.transform.position = ArenaSpace.ToWorld(position) + Vector3.up * 0.03f;
            go.transform.localScale = new Vector3(radius * 0.3f, 0.03f, radius * 0.3f);
            DeathBurstView burst = new DeathBurstView();
            burst.Go = go;
            burst.Elapsed = 0f;
            burst.Duration = 0.25f;
            burst.Radius = radius;
            _bursts.Add(burst);
        }

        private void UpdateBursts(float dt)
        {
            for (int i = _bursts.Count - 1; i >= 0; i--)
            {
                DeathBurstView burst = _bursts[i];
                burst.Elapsed += dt;
                float t = burst.Duration <= 0f ? 1f : Mathf.Clamp01(burst.Elapsed / burst.Duration);
                float scale = burst.Radius * 2f * Mathf.Lerp(0.15f, 1f, t);
                burst.Go.transform.localScale = new Vector3(scale, 0.03f, scale);
                if (burst.Elapsed >= burst.Duration)
                {
                    Object.Destroy(burst.Go);
                    _bursts.RemoveAt(i);
                }
            }
        }

        private static Color PickupColor(int upgradeIndex)
        {
            switch (upgradeIndex)
            {
                case 0: return new Color(0.9f, 0.72f, 0.3f);
                case 1: return new Color(0.25f, 0.45f, 0.85f);
                default: return new Color(0.3f, 0.7f, 0.68f);
            }
        }

        private void RenderAimMarkers(CombatSimulation sim)
        {
            if (_aimMarkers.Count == 0) return;
            for (int i = 0; i < sim.Players.Count; i++)
            {
                PlayerState p = sim.Players[i];
                Transform t;
                if (!_aimMarkers.TryGetValue(p.Id, out t)) continue;
                bool show = !p.IsDefeated && p.HasAimTarget;
                t.gameObject.SetActive(show);
                if (!show) continue;
                t.position = ArenaSpace.ToWorld(p.AimTarget) + Vector3.up * 0.03f;
                PlaceholderVisuals.SetColor(_aimMarkerRenderers[p.Id], p.AimTargetClamped ? TargetClampedColor : TargetColor);
            }
        }

        public void ConsumeEvents(IReadOnlyList<SimEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                SimEvent e = events[i];
                if (e.Kind == SimEventKind.DamageDealt)
                    _hitTimers[e.TargetId] = 0.12f;
            }
        }

        private void RenderPlayers(CombatSimulation sim)
        {
            for (int i = 0; i < sim.Players.Count; i++)
            {
                PlayerState p = sim.Players[i];
                Transform t;
                if (!_players.TryGetValue(p.Id, out t)) continue;
                bool alive = !p.IsDefeated;
                t.gameObject.SetActive(alive);
                if (!alive) continue;
                t.position = ArenaSpace.ToWorld(p.Position) + Vector3.up * 0.45f;
                t.rotation = Quaternion.Euler(0f, ArenaSpace.ToYaw(p.Facing), 0f);
            }
        }

        private void RenderEnemies(CombatSimulation sim, float dt)
        {
            for (int i = 0; i < sim.Enemies.Count; i++)
            {
                EnemyState e = sim.Enemies[i];
                Transform t;
                if (!_enemies.TryGetValue(e.Id, out t)) continue;

                if (e.IsDefeated)
                {
                    if (_defeatedEnemies.Add(e.Id) && e.Spec != null && e.Spec.DeathBurstRadius > 0f)
                        SpawnDeathBurst(e.Position, e.Spec.DeathBurstRadius);
                }
                else
                {
                    _defeatedEnemies.Remove(e.Id);
                }

                bool alive = !e.IsDefeated;
                t.gameObject.SetActive(alive);
                if (!alive) continue;

                t.position = ArenaSpace.ToWorld(e.Position) + Vector3.up * e.Spec.BodyRadius;
                t.rotation = Quaternion.Euler(0f, ArenaSpace.ToYaw(e.Facing), 0f);

                float punch = 1f;
                float hit;
                if (_hitTimers.TryGetValue(e.Id, out hit) && hit > 0f)
                {
                    punch = 1f + 0.3f * (hit / 0.12f);
                    hit -= dt;
                    if (hit <= 0f) _hitTimers.Remove(e.Id);
                    else _hitTimers[e.Id] = hit;
                }
                t.localScale = Vector3.one * (e.Spec.BodyRadius * 2f * punch);

                Renderer renderer = _enemyRenderers[e.Id];
                PlaceholderVisuals.SetColor(renderer, e.IsRestrained ? RestrainedColor : EnemyColor);
            }
        }

        private void SyncCages(CombatSimulation sim)
        {
            _seen.Clear();
            for (int i = 0; i < sim.Cages.Count; i++)
            {
                RootCageState c = sim.Cages[i];
                _seen.Add(c.Id);

                Transform t;
                if (!_cages.TryGetValue(c.Id, out t))
                {
                    GameObject go = PlaceholderVisuals.CreateCylinder("Cage_" + c.Id, CageColor);
                    go.transform.SetParent(transform, false);
                    t = go.transform;
                    _cages[c.Id] = t;
                    _cageRenderers[c.Id] = go.GetComponent<Renderer>();
                }

                float diameter = c.Radius * 2f;
                t.position = ArenaSpace.ToWorld(c.Position) + Vector3.up * 0.05f;
                t.localScale = new Vector3(diameter, 0.05f, diameter);
                PlaceholderVisuals.SetColor(_cageRenderers[c.Id], c.IsIgnited ? CageIgnitedColor : CageColor);
            }
            RemoveStale(_cages, _cageRenderers);
        }

        private void SyncProjectiles(CombatSimulation sim)
        {
            _seen.Clear();
            for (int i = 0; i < sim.Projectiles.Count; i++)
            {
                ProjectileState p = sim.Projectiles[i];
                _seen.Add(p.Id);

                Transform t;
                if (!_projectiles.TryGetValue(p.Id, out t))
                {
                    Transform created = PlaceholderVisuals.CreateSphere("Projectile_" + p.Id, ProjectileColor).transform;
                    created.SetParent(transform, false);
                    _projectiles[p.Id] = created;
                    t = created;
                }
                t.position = ArenaSpace.ToWorld(p.Position) + Vector3.up * 0.4f;
                t.localScale = Vector3.one * (p.Radius * 2f);
            }
            RemoveStale(_projectiles, null);
        }

        private void RemoveStale(Dictionary<int, Transform> collection, Dictionary<int, Renderer> renderers)
        {
            _stale.Clear();
            foreach (KeyValuePair<int, Transform> pair in collection)
            {
                if (!_seen.Contains(pair.Key)) _stale.Add(pair.Key);
            }
            for (int i = 0; i < _stale.Count; i++)
            {
                int id = _stale[i];
                Transform t = collection[id];
                if (t != null) Object.Destroy(t.gameObject);
                collection.Remove(id);
                if (renderers != null) renderers.Remove(id);
            }
        }

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Object.Destroy(transform.GetChild(i).gameObject);
        }
    }
}
