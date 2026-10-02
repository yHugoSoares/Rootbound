using UnityEngine;
using UnityEngine.InputSystem;
using Rootbound.Core;

namespace Rootbound.Unity
{
    public sealed class CombatHud : MonoBehaviour
    {
        public bool showDiagnostics = false;

        private CombatSimulation _sim;
        private GUIStyle _style;
        private GUIStyle _warnStyle;

        public void Bind(CombatSimulation sim)
        {
            _sim = sim;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
                showDiagnostics = !showDiagnostics;
        }

        private void OnGUI()
        {
            if (_sim == null) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label);
                _style.fontSize = 14;
                _style.normal.textColor = Color.white;
                _warnStyle = new GUIStyle(GUI.skin.label);
                _warnStyle.fontSize = 14;
                _warnStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
            }

            DrawPlayers();
            DrawEncounter();
            DrawDiagnostics();

            if (_sim.EncounterCleared || _sim.AllPlayersDefeated)
            {
                string title = _sim.EncounterCleared ? "Region Reclaimed" : "All Creatures Down";
                string subtitle = _sim.EncounterCleared ? "Press R to run it back" : "Press R to restart";
                Rect box = new Rect(Screen.width * 0.5f - 190f, 70f, 380f, 64f);
                GUI.color = new Color(0f, 0f, 0f, 0.7f);
                GUI.Box(box, GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(new Rect(box.x + 16f, box.y + 8f, box.width, 24f), title, _style);
                GUI.Label(new Rect(box.x + 16f, box.y + 32f, box.width, 24f), subtitle, _style);
            }
        }

        private void DrawPlayers()
        {
            float x = 16f;
            float y = 16f;
            for (int i = 0; i < _sim.Players.Count; i++)
            {
                PlayerState p = _sim.Players[i];
                float width = 250f;
                GUI.Label(new Rect(x, y, width, 20f), p.Spec.DisplayName, _style);
                DrawBar(new Rect(x, y + 22f, width, 14f), p.Health.Normalized, HealthColor(p.Health.Normalized));
                GUI.Label(new Rect(x, y + 38f, width + 220f, 20f),
                    "HP " + Mathf.CeilToInt(p.Health.Current) + "/" + Mathf.CeilToInt(p.Health.Max) +
                    "   Dodge " + CooldownDisplay.Label(p.DodgeCooldown) +
                    "   Primary " + CooldownDisplay.Label(p.PrimaryCooldown) +
                    "   Special " + CooldownDisplay.Label(p.SpecialCooldown), _style);

                if (p.LastRequestTick >= 0 && !p.LastRequestAccepted)
                {
                    float age = (_sim.Tick - p.LastRequestTick) * _sim.DeltaTime;
                    if (age <= 0.9f)
                        GUI.Label(new Rect(x, y + 60f, width + 220f, 18f),
                            p.LastRequestedSlot + ": " + p.LastRequestReason, _warnStyle);
                }
                y += 84f;
            }
        }

        private void DrawEncounter()
        {
            int alive = 0;
            for (int i = 0; i < _sim.Enemies.Count; i++)
                if (!_sim.Enemies[i].IsDefeated) alive++;

            float x = Screen.width - 230f;
            float y = 16f;
            GUI.Label(new Rect(x, y, 214f, 20f), "Blightlings: " + alive + " / " + _sim.Enemies.Count, _style);

            for (int i = 0; i < _sim.Cages.Count; i++)
            {
                RootCageState cage = _sim.Cages[i];
                string state = cage.IsIgnited ? " ignited " + cage.IgnitedTimeRemaining.ToString("0.0") + "s" : "";
                GUI.Label(new Rect(x, y + 22f + i * 20f, 214f, 20f),
                    "Root Cage " + cage.Remaining.ToString("0.0") + "s" + state, _style);
            }
        }

        private void DrawDiagnostics()
        {
            if (!showDiagnostics) return;
            if (!Application.isEditor && !Debug.isDebugBuild) return;

            float x = 16f;
            float y = Screen.height - 190f;
            float width = 620f;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.Box(new Rect(x - 6f, y - 6f, width + 12f, 182f), GUIContent.none);
            GUI.color = Color.white;

            GUI.Label(new Rect(x, y, width, 20f),
                "DIAG (F1)  tick=" + _sim.Tick +
                "  elapsed=" + _sim.ElapsedTime.ToString("0.00") + "s" +
                "  dt=" + _sim.DeltaTime.ToString("0.0000") + "s" +
                "  timeScale=" + Time.timeScale.ToString("0.00"), _style);
            y += 22f;

            for (int i = 0; i < _sim.Players.Count; i++)
            {
                PlayerState p = _sim.Players[i];
                GUI.Label(new Rect(x, y, width, 20f),
                    p.Spec.DisplayName +
                    "  HP=" + p.Health.Current.ToString("0") + "/" + p.Health.Max.ToString("0") +
                    "  defeated=" + p.IsDefeated +
                    "  dodge=" + (p.Dodge.IsActive ? "active" : "idle") +
                    "/invuln=" + p.Dodge.IsInvulnerable, _style);
                GUI.Label(new Rect(x, y + 18f, width, 20f),
                    "   cd P=" + CooldownDisplay.Label(p.PrimaryCooldown) +
                    " S=" + CooldownDisplay.Label(p.SpecialCooldown) +
                    " D=" + CooldownDisplay.Label(p.DodgeCooldown) +
                    " normD=" + CooldownDisplay.NormalizedLabel(p.DodgeCooldown) +
                    "   last=" + p.LastRequestedSlot +
                    " " + (p.LastRequestAccepted ? "accepted" : "rejected") +
                    " (" + p.LastRequestReason + ") @tick " + p.LastRequestTick, _style);
                y += 40f;
            }

            for (int i = 0; i < _sim.Enemies.Count && i < 2; i++)
            {
                EnemyState e = _sim.Enemies[i];
                GUI.Label(new Rect(x, y, width, 20f),
                    "Enemy " + e.Id +
                    "  HP=" + e.Health.Current.ToString("0") +
                    "  defeated=" + e.IsDefeated +
                    "  attackCd=" + e.AttackCooldown.Remaining.ToString("0.00") +
                    "  restrained=" + e.IsRestrained, _style);
                y += 20f;
            }
        }

        private static Color HealthColor(float normalized)
        {
            return Color.Lerp(new Color(0.85f, 0.2f, 0.15f), new Color(0.25f, 0.8f, 0.3f), normalized);
        }

        private static void DrawBar(Rect rect, float normalized, Color color)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.Box(rect, GUIContent.none);
            if (normalized > 0f)
            {
                GUI.color = color;
                GUI.Box(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(normalized), rect.height), GUIContent.none);
            }
            GUI.color = Color.white;
        }
    }
}
