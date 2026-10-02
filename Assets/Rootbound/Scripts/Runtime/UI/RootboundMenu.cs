using UnityEngine;

namespace Rootbound.Unity
{
    public sealed class RootboundMenu : MonoBehaviour
    {
        public LocalGameRunner runner;
        public int localPlayerCount = 2;

        private INetworkSession _session;
        private string _joinCode = string.Empty;
        private bool _started;
        private GUIStyle _title;
        private GUIStyle _label;

        private void Awake()
        {
            if (runner == null) runner = FindFirstObjectByType<LocalGameRunner>();
            _session = new OfflineNetworkSession();
            _session.Changed += OnSessionChanged;
        }

        private void OnDestroy()
        {
            if (_session != null) _session.Changed -= OnSessionChanged;
        }

        private void OnSessionChanged()
        {
            Repaint();
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (_started) DrawStatus();
            else DrawMenu();
        }

        private void DrawMenu()
        {
            float width = 440f;
            Rect panel = new Rect(Screen.width * 0.5f - width * 0.5f, Screen.height * 0.5f - 160f, width, 306f);
            GUI.color = new Color(0f, 0f, 0f, 0.78f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;

            GUI.Label(new Rect(panel.x + 20f, panel.y + 16f, width - 40f, 34f), "Rootbound: Fractured Realms", _title);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 56f, width - 40f, 22f), "Milestone 1 - Local Combat Arena", _label);

            if (GUI.Button(new Rect(panel.x + 20f, panel.y + 96f, width - 40f, 36f), "Host Local Session (" + localPlayerCount + " players)"))
                StartLocal();

            GUI.Label(new Rect(panel.x + 20f, panel.y + 146f, width - 40f, 22f), "Join session code", _label);
            _joinCode = GUI.TextField(new Rect(panel.x + 20f, panel.y + 170f, width - 130f, 28f), _joinCode);
            if (GUI.Button(new Rect(panel.x + width - 100f, panel.y + 170f, 80f, 28f), "Join"))
                _session.Join(_joinCode);

            if (!string.IsNullOrEmpty(_session.LastError))
                GUI.Label(new Rect(panel.x + 20f, panel.y + 206f, width - 40f, 42f), _session.LastError, _label);

            if (GUI.Button(new Rect(panel.x + 20f, panel.y + 256f, 120f, 30f), "Quit"))
                Application.Quit();
        }

        private void StartLocal()
        {
            _session.StartHost();
            if (runner != null)
            {
                runner.playerCount = localPlayerCount;
                runner.BeginSession();
            }
            _started = true;
        }

        public void ReturnToMenu()
        {
            if (runner != null) runner.EndSession();
            _session.Leave();
            _started = false;
        }

        private void DrawStatus()
        {
            string status = _session.IsHost ? "Hosting " + _session.SessionCode : _session.State.ToString();
            GUI.Label(new Rect(16f, Screen.height - 28f, 520f, 22f), "Session: " + status + " (offline/local)", _label);
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
