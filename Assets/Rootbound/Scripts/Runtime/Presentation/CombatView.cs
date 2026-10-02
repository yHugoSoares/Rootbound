using System.Collections.Generic;
using UnityEngine;
using Rootbound.Core;

namespace Rootbound.Unity
{
    public sealed class CombatView : MonoBehaviour
    {
        private static readonly Color RootColor = new Color(0.32f, 0.6f, 0.28f);
        private static readonly Color MothColor = new Color(0.95f, 0.45f, 0.15f);
        private static readonly Color EnemyColor = new Color(0.55f, 0.2f, 0.62f);
        private static readonly Color RestrainedColor = new Color(0.35f, 0.25f, 0.5f);
        private static readonly Color CageColor = new Color(0.25f, 0.7f, 0.35f);
        private static readonly Color CageIgnitedColor = new Color(1f, 0.45f, 0.1f);
        private static readonly Color ProjectileColor = new Color(1f, 0.75f, 0.2f);
        private static readonly Color TargetColor = new Color(0.4f, 0.9f, 1f);
        private static readonly Color TargetClampedColor = new Color(1f, 0.6f, 0.2f);

        private readonly Dictionary<int, Transform> _aimMarkers = new Dictionary<int, Transform>();
        private readonly Dictionary<int, Renderer> _aimMarkerRenderers = new Dictionary<int, Renderer>();
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
            RenderAimMarkers(sim);
            SyncCages(sim);
            SyncProjectiles(sim);
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
