using UnityEngine;
using Rootbound.Core;

namespace Rootbound.Unity
{
    public sealed class CombatHud : MonoBehaviour
    {
        private CombatSimulation _sim;
        private GUIStyle _style;

        public void Bind(CombatSimulation sim)
        {
            _sim = sim;
        }

        private void OnGUI()
        {
            if (_sim == null) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label);
                _style.fontSize = 14;
                _style.normal.textColor = Color.white;
            }

            DrawPlayers();
            DrawEncounter();

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
                GUI.Label(new Rect(x, y + 38f, width, 20f),
                    "HP " + Mathf.CeilToInt(p.Health.Current) + "/" + Mathf.CeilToInt(p.Health.Max) +
                    "   Dodge " + ReadyText(p.DodgeCooldown) +
                    "   Primary " + ReadyText(p.PrimaryCooldown) +
                    "   Special " + ReadyText(p.SpecialCooldown), _style);
                y += 66f;
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

        private static string ReadyText(Cooldown cooldown)
        {
            return cooldown.IsReady ? "ready" : cooldown.Remaining.ToString("0.0");
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
