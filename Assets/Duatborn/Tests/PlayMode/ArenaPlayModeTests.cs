using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Duatborn.Core;
using Duatborn.Unity;

namespace Duatborn.Tests
{
    public class ArenaPlayModeTests
    {
        [UnityTest]
        public IEnumerator ArenaSceneRunsLocalSession()
        {
            SceneManager.LoadScene("CombatArena");
            yield return null;
            yield return null;

            LocalGameRunner runner = Object.FindFirstObjectByType<LocalGameRunner>();
            Assert.That(runner, Is.Not.Null, "LocalGameRunner missing from CombatArena scene.");
            Assert.That(runner.view, Is.Not.Null, "CombatView reference is not wired.");
            Assert.That(runner.hud, Is.Not.Null, "CombatHud reference is not wired.");
            Assert.That(runner.cameraRig, Is.Not.Null, "IsometricCameraRig reference is not wired.");
            Assert.That(runner.arenaCamera, Is.Not.Null, "Arena camera reference is not wired.");
            Assert.That(runner.IsRunning, Is.False, "Simulation should not run before BeginSession.");

            runner.BeginSession();
            Assert.That(runner.Simulation, Is.Not.Null);
            Assert.That(runner.IsRunning, Is.True);

            Vec2[] spawnPositions = new Vec2[runner.Simulation.Enemies.Count];
            for (int i = 0; i < spawnPositions.Length; i++)
                spawnPositions[i] = runner.Simulation.Enemies[i].Position;

            yield return new WaitForSeconds(6.0f);

            Assert.That(runner.Simulation.Tick, Is.GreaterThan(30), "Simulation did not advance enough in Play Mode.");
            Assert.That(runner.Simulation.Players.Count, Is.EqualTo(2));
            Assert.That(runner.Simulation.Enemies.Count, Is.EqualTo(6));
            Assert.That(runner.Simulation.GetPlayer(0).Spec.Kind, Is.EqualTo(CreatureKind.DuneWarden));
            Assert.That(runner.Simulation.GetPlayer(1).Spec.Kind, Is.EqualTo(CreatureKind.Sunwing));

            bool anyEnemyMoved = false;
            for (int i = 0; i < runner.Simulation.Enemies.Count; i++)
            {
                if (Vec2.Distance(spawnPositions[i], runner.Simulation.Enemies[i].Position) > 0.5f)
                {
                    anyEnemyMoved = true;
                    break;
                }
            }
            Assert.That(anyEnemyMoved, Is.True, "HollowSentinel AI did not move any enemy toward the players.");

            bool anyPlayerDamaged = false;
            for (int i = 0; i < runner.Simulation.Players.Count; i++)
            {
                if (runner.Simulation.Players[i].Health.Current < runner.Simulation.Players[i].Health.Max)
                {
                    anyPlayerDamaged = true;
                    break;
                }
            }
            Assert.That(anyPlayerDamaged, Is.True, "HollowSentinel attacks did not damage any player in Play Mode.");
        }

        [UnityTest]
        public IEnumerator RunnerAdvancesCooldownsWithoutInput()
        {
            SceneManager.LoadScene("CombatArena");
            yield return null;
            yield return null;

            LocalGameRunner runner = Object.FindFirstObjectByType<LocalGameRunner>();
            Assert.That(runner, Is.Not.Null);
            runner.BeginSession();

            PlayerState player = runner.Simulation.GetPlayer(0);
            player.PrimaryCooldown.TryStart(0.2f);
            player.SpecialCooldown.TryStart(0.2f);
            player.DodgeCooldown.TryStart(0.2f);

            Assert.That(player.PrimaryCooldown.IsReady, Is.False);
            Assert.That(player.SpecialCooldown.IsReady, Is.False);
            Assert.That(player.DodgeCooldown.IsReady, Is.False);

            yield return new WaitForSeconds(1.0f);

            Assert.That(runner.Simulation.ElapsedTime, Is.GreaterThan(0.2f), "Simulation elapsed time did not advance.");
            Assert.That(player.PrimaryCooldown.IsReady, Is.True, "Primary cooldown did not recover through LocalGameRunner.");
            Assert.That(player.SpecialCooldown.IsReady, Is.True, "Special cooldown did not recover through LocalGameRunner.");
            Assert.That(player.DodgeCooldown.IsReady, Is.True, "Dodge cooldown did not recover through LocalGameRunner.");
        }

        [Test]
        public void DodgePressIsLatchedAcrossFramesWithoutSimulationSteps()
        {
            DuatbornInputActions actions = new DuatbornInputActions(0);
            PlayerInputAdapter adapter = new PlayerInputAdapter(actions, null);

            adapter.CaptureFrame(true);
            adapter.CaptureFrame(false);

            PlayerCommand first = adapter.Build(Vector3.zero);
            PlayerCommand second = adapter.Build(Vector3.zero);

            Assert.That(first.Dodge, Is.True, "A dodge press on a frame with no fixed step was lost.");
            Assert.That(second.Dodge, Is.False, "The latched dodge press was consumed more than once.");

            actions.Dispose();
        }

        [Test]
        public void CameraFocusesConfiguredLocalPlayer()
        {
            CombatSimulation sim = new CombatSimulation(CombatSetup.DefaultTwoPlayer());

            GameObject go = new GameObject("CameraRigTest");
            Camera camera = go.AddComponent<Camera>();
            IsometricCameraRig rig = go.AddComponent<IsometricCameraRig>();
            rig.targetCamera = camera;
            rig.Bind(sim);

            rig.SetLocalPlayer(1);
            Vector3 expected = ArenaSpace.ToWorld(sim.GetPlayer(1).Position);
            Assert.That(Vector3.Distance(rig.FocusCenter, expected), Is.LessThan(0.01f));

            rig.SetLocalPlayer(-1);
            Vector3 p0 = ArenaSpace.ToWorld(sim.GetPlayer(0).Position);
            Vector3 p1 = ArenaSpace.ToWorld(sim.GetPlayer(1).Position);
            Vector3 centroid = (p0 + p1) * 0.5f;
            Assert.That(Vector3.Distance(rig.FocusCenter, centroid), Is.LessThan(0.01f));

            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator SoloRunnerSpawnsOneSelectedCreatureAndCycles()
        {
            SceneManager.LoadScene("CombatArena");
            yield return null;
            yield return null;

            LocalGameRunner runner = Object.FindFirstObjectByType<LocalGameRunner>();
            runner.playerCount = 1;
            runner.selectedCreature = CreatureKind.Sunwing;
            runner.pauseAllowed = true;
            runner.BeginSession();

            Assert.That(runner.Simulation.Players.Count, Is.EqualTo(1));
            Assert.That(runner.Simulation.Players[0].Spec.Kind, Is.EqualTo(CreatureKind.Sunwing));
            Assert.That(runner.cameraRig.localPlayerId, Is.EqualTo(0));

            runner.EndSession();
            Assert.That(runner.IsRunning, Is.False);
            Assert.That(runner.Simulation, Is.Null);

            runner.selectedCreature = CreatureKind.DuneWarden;
            runner.BeginSession();
            Assert.That(runner.Simulation.Players.Count, Is.EqualTo(1));
            Assert.That(runner.Simulation.Players[0].Spec.Kind, Is.EqualTo(CreatureKind.DuneWarden));
            runner.EndSession();
        }

        [UnityTest]
        public IEnumerator SoloPauseStopsLocalSimulation()
        {
            SceneManager.LoadScene("CombatArena");
            yield return null;
            yield return null;

            LocalGameRunner runner = Object.FindFirstObjectByType<LocalGameRunner>();
            runner.playerCount = 1;
            runner.selectedCreature = CreatureKind.DuneWarden;
            runner.pauseAllowed = true;
            runner.BeginSession();
            yield return new WaitForSeconds(0.4f);

            int before = runner.Simulation.Tick;
            runner.SetPaused(true);
            yield return new WaitForSeconds(0.4f);

            Assert.That(runner.IsPaused, Is.True);
            Assert.That(runner.Simulation.Tick, Is.EqualTo(before), "Paused solo simulation advanced.");

            runner.SetPaused(false);
            yield return new WaitForSeconds(0.4f);
            Assert.That(runner.Simulation.Tick, Is.GreaterThan(before));
            runner.EndSession();
        }

        [UnityTest]
        public IEnumerator OnlineHostModeIgnoresPause()
        {
            SceneManager.LoadScene("CombatArena");
            yield return null;
            yield return null;

            LocalGameRunner runner = Object.FindFirstObjectByType<LocalGameRunner>();
            runner.playerCount = 1;
            runner.pauseAllowed = false;
            runner.BeginSession();
            yield return new WaitForSeconds(0.4f);

            int before = runner.Simulation.Tick;
            runner.SetPaused(true);
            yield return new WaitForSeconds(0.4f);

            Assert.That(runner.IsPaused, Is.False);
            Assert.That(runner.Simulation.Tick, Is.GreaterThan(before));
            runner.EndSession();
        }
    }
}
