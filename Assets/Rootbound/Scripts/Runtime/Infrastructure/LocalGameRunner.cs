using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Rootbound.Core;

namespace Rootbound.Unity
{
    public sealed class LocalGameRunner : MonoBehaviour
    {
        public CreatureDefinitionAsset player0Definition;
        public CreatureDefinitionAsset player1Definition;
        public EnemyDefinitionAsset enemyDefinition;
        public int enemyCount = 6;
        public float arenaRadius = 18f;
        public int playerCount = 2;
        public CreatureKind selectedCreature = CreatureKind.RootGuardian;
        public bool pauseAllowed;
        public Camera arenaCamera;
        public CombatView view;
        public CombatHud hud;
        public IsometricCameraRig cameraRig;

        public CombatSimulation Simulation { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }

        private readonly List<RootboundInputActions> _actions = new List<RootboundInputActions>();
        private readonly List<PlayerInputAdapter> _adapters = new List<PlayerInputAdapter>();
        private const int MaxStepsPerFrame = 5;
        private float _accumulator;

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
                RootboundInputActions actions = new RootboundInputActions(i);
                actions.Enable();
                _actions.Add(actions);
                _adapters.Add(new PlayerInputAdapter(actions, arenaCamera));
            }

            Simulation = new CombatSimulation(BuildSetup());
            if (view != null) view.Bind(Simulation);
            if (hud != null) hud.Bind(Simulation);
            if (cameraRig != null)
            {
                cameraRig.Bind(Simulation);
                cameraRig.SetLocalPlayer(playerCount <= 1 ? 0 : -1);
            }
            _accumulator = 0f;
            IsPaused = false;
            IsRunning = true;
        }

        public void SetPaused(bool paused)
        {
            if (!pauseAllowed) return;
            IsPaused = paused;
        }

        public void Restart()
        {
            if (Simulation == null) return;
            Simulation.Reset();
            if (view != null) view.Bind(Simulation);
            if (hud != null) hud.Bind(Simulation);
            if (cameraRig != null) cameraRig.Bind(Simulation);
            _accumulator = 0f;
        }

        public void EndSession()
        {
            for (int i = 0; i < _actions.Count; i++) _actions[i].Dispose();
            _actions.Clear();
            _adapters.Clear();
            IsRunning = false;
            IsPaused = false;
            Simulation = null;
        }

        private CreatureSpec SoloSpec()
        {
            if (selectedCreature == CreatureKind.EmberMoth)
                return player1Definition != null ? player1Definition.spec : DefaultContent.EmberMoth();
            return player0Definition != null ? player0Definition.spec : DefaultContent.RootGuardian();
        }

        private CombatSetup BuildSetup()
        {
            int count = Mathf.Max(1, playerCount);
            CreatureSpec[] specs = new CreatureSpec[count];
            if (count == 1)
            {
                specs[0] = SoloSpec();
            }
            else
            {
                specs[0] = player0Definition != null ? player0Definition.spec : DefaultContent.RootGuardian();
                specs[1] = player1Definition != null ? player1Definition.spec : DefaultContent.EmberMoth();
            }

            CombatSetup setup = new CombatSetup();
            setup.PlayerSpecs = specs;
            setup.EnemySpec = enemyDefinition != null ? enemyDefinition.spec : DefaultContent.Blightling();
            setup.EnemyCount = enemyCount;
            setup.ArenaRadius = arenaRadius;
            return setup;
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

            if (view != null) view.Render(Simulation, Time.deltaTime);

            bool finished = Simulation.EncounterCleared || Simulation.AllPlayersDefeated;
            if (finished && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                Restart();
        }

        private void OnDestroy()
        {
            EndSession();
        }
    }
}
