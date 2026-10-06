using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Duatborn.Core;
using Duatborn.Unity;
using Duatborn.Fusion;

namespace Duatborn.Tests
{
    // Host-authoritative replication over the real Photon transport (Fusion
    // Multi-Peer). All checks share ONE connection: movement, attack, health,
    // cage and ignition. Repeated connect/shutdown cycles in one process were
    // flaky, so they are combined.
    public class FusionMovementReplicationTests
    {
        private FusionNetworkSession _host;
        private FusionNetworkSession _client;

        private static FusionNetworkSession CreateSession(string name)
        {
            GameObject go = new GameObject(name);
            return go.AddComponent<FusionNetworkSession>();
        }

        private static IEnumerator WaitUntil(Func<bool> condition, float timeout)
        {
            float elapsed = 0f;
            while (!condition() && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private void Cleanup()
        {
            if (_client != null)
            {
                _client.Leave();
                UnityEngine.Object.Destroy(_client.gameObject);
                _client = null;
            }
            if (_host != null)
            {
                _host.Leave();
                UnityEngine.Object.Destroy(_host.gameObject);
                _host = null;
            }
        }

        private IEnumerator ConnectAndFindMatches(Action<FusionCombatHost, FusionCombatHost> onReady)
        {
            _host = CreateSession("ReplicationHost");
            yield return null;
            _host.StartHost();
            yield return WaitUntil(() => _host.State == NetworkSessionState.Connected, 45f);
            Assert.That(_host.State, Is.EqualTo(NetworkSessionState.Connected), "Host: " + _host.LastError);
            yield return new WaitForSeconds(1.5f);

            _client = CreateSession("ReplicationClient");
            yield return null;
            _client.Join(_host.SessionCode);
            yield return WaitUntil(() => _client.State == NetworkSessionState.Connected, 45f);
            if (_client.State != NetworkSessionState.Connected)
            {
                yield return new WaitForSeconds(1.5f);
                _client.Join(_host.SessionCode);
                yield return WaitUntil(() => _client.State == NetworkSessionState.Connected, 45f);
            }
            Assert.That(_client.State, Is.EqualTo(NetworkSessionState.Connected), "Client: " + _client.LastError);

            FusionCombatHost hostMatch = null;
            FusionCombatHost clientMatch = null;
            yield return WaitUntil(() =>
            {
                hostMatch = null;
                clientMatch = null;
                FusionCombatHost[] all = UnityEngine.Object.FindObjectsByType<FusionCombatHost>(FindObjectsSortMode.None);
                foreach (FusionCombatHost candidate in all)
                {
                    if (candidate.HasStateAuthority) hostMatch = candidate;
                    else clientMatch = candidate;
                }
                return hostMatch != null && clientMatch != null;
            }, 15f);

            Assert.That(hostMatch, Is.Not.Null, "Host match object did not spawn.");
            Assert.That(clientMatch, Is.Not.Null, "Client did not receive a match proxy.");
            onReady(hostMatch, clientMatch);
        }

        [UnityTest]
        public IEnumerator AllCombatStateReplicatesToClient()
        {
            LogAssert.ignoreFailingMessages = true;
            FusionCombatHost hostMatch = null;
            FusionCombatHost clientMatch = null;
            try
            {
                yield return ConnectAndFindMatches((h, c) => { hostMatch = h; clientMatch = c; });
                _client.LocalCreature = CreatureKind.Sunwing;
                yield return new WaitForSeconds(0.5f);

                // --- Movement ---
                float startY = hostMatch.Simulation.GetPlayer(0).Position.Y;
                hostMatch.HasHostCommandOverride = true;
                hostMatch.HostCommandOverride = new PlayerCommand { Move = new Vec2(0f, 1f) };
                yield return WaitUntil(() => hostMatch.Simulation.GetPlayer(0).Position.Y > startY + 0.2f, 8f);
                Assert.That(hostMatch.Simulation.GetPlayer(0).Position.Y, Is.GreaterThan(startY + 0.2f),
                    "Host player did not move.");
                yield return WaitUntil(() => clientMatch.NetPlayer0Position.z > startY + 0.2f, 8f);
                hostMatch.HasHostCommandOverride = false;
                yield return new WaitForSeconds(0.6f);
                Assert.That(clientMatch.NetPlayer0Position.z,
                    Is.EqualTo(hostMatch.Simulation.GetPlayer(0).Position.Y).Within(0.3f),
                    "Movement did not replicate.");

                // --- Lobby has no enemies until the host starts the encounter ---
                Assert.That(hostMatch.Simulation.Enemies.Count, Is.EqualTo(0), "Lobby should start without enemies.");
                Assert.That(clientMatch.Simulation.Enemies.Count, Is.EqualTo(0), "Client lobby should have no enemies.");
                hostMatch.StartEncounter();
                yield return WaitUntil(() => hostMatch.IsEncounterStarted && hostMatch.Simulation.Enemies.Count > 0, 8f);
                yield return WaitUntil(() => clientMatch.IsEncounterStarted && clientMatch.Simulation.Enemies.Count > 0, 8f);
                Assert.That(clientMatch.Simulation.Enemies.Count, Is.EqualTo(hostMatch.Simulation.Enemies.Count),
                    "Encounter start did not replicate to the client.");

                // --- Attack (host primary against an adjacent enemy) ---
                hostMatch.Simulation.Enemies[0].Position =
                    hostMatch.Simulation.GetPlayer(0).Position + new Vec2(0f, 0.6f);
                float enemyBefore = hostMatch.Simulation.Enemies[0].Health.Current;
                hostMatch.HasHostCommandOverride = true;
                hostMatch.HostCommandOverride = new PlayerCommand { Aim = new Vec2(0f, 1f), Primary = true };
                yield return WaitUntil(() => hostMatch.Simulation.Enemies[0].Health.Current < enemyBefore, 8f);
                Assert.That(hostMatch.Simulation.Enemies[0].Health.Current, Is.LessThan(enemyBefore),
                    "Host attack did not damage the enemy.");
                yield return WaitUntil(() => clientMatch.Simulation.Enemies[0].Health.Current < enemyBefore, 8f);
                Assert.That(clientMatch.Simulation.Enemies[0].Health.Current,
                    Is.EqualTo(hostMatch.Simulation.Enemies[0].Health.Current).Within(0.05f),
                    "Attack damage did not replicate.");
                hostMatch.HasHostCommandOverride = false;

                // --- Player health ---
                hostMatch.Simulation.GetPlayer(0).Health.ApplyDamage(20f);
                float playerHealth = hostMatch.Simulation.GetPlayer(0).Health.Current;
                yield return WaitUntil(() => Mathf.Abs(clientMatch.Simulation.GetPlayer(0).Health.Current - playerHealth) < 0.05f, 8f);
                Assert.That(clientMatch.Simulation.GetPlayer(0).Health.Current,
                    Is.EqualTo(playerHealth).Within(0.05f), "Player health did not replicate.");

                // --- Cage ---
                Vec2 target = hostMatch.Simulation.GetPlayer(1).Position + new Vec2(0f, 3f);
                hostMatch.HasHostCommandOverride = true;
                hostMatch.HostCommandOverride = new PlayerCommand
                {
                    Special = true,
                    HasTargetPoint = true,
                    TargetPoint = target
                };
                yield return WaitUntil(() => hostMatch.Simulation.Cages.Count > 0, 8f);
                Assert.That(hostMatch.Simulation.Cages.Count, Is.GreaterThan(0), "Host cage was not created.");
                yield return WaitUntil(() => clientMatch.Simulation.Cages.Count > 0, 8f);
                Assert.That(clientMatch.Simulation.Cages.Count, Is.GreaterThan(0), "Cage did not replicate.");
                Vec2 cagePosition = hostMatch.Simulation.Cages[0].Position;
                Assert.That(Vec2.Distance(clientMatch.Simulation.Cages[0].Position, cagePosition),
                    Is.LessThan(0.3f), "Replicated cage position is wrong.");
                hostMatch.HasHostCommandOverride = false;

                // --- Ignition (player 1 Ember Moth ignites the cage) ---
                hostMatch.HasHostCommandOverride2 = true;
                hostMatch.HostCommandOverride2 = new PlayerCommand
                {
                    Special = true,
                    HasTargetPoint = true,
                    TargetPoint = cagePosition
                };
                yield return WaitUntil(() => hostMatch.Simulation.Cages.Count > 0
                    && hostMatch.Simulation.Cages[0].IsIgnited, 8f);
                Assert.That(hostMatch.Simulation.Cages[0].IsIgnited, Is.True, "Host cage was not ignited.");
                yield return WaitUntil(() => clientMatch.Simulation.Cages.Count > 0
                    && clientMatch.Simulation.Cages[0].IsIgnited, 8f);
                Assert.That(clientMatch.Simulation.Cages[0].IsIgnited, Is.True, "Ignition did not replicate.");

                // --- Room advance (upgrade choice) replicates ---
                for (int i = 0; i < hostMatch.Simulation.Enemies.Count; i++)
                    hostMatch.Simulation.Enemies[i].Health.ApplyDamage(1000f);
                yield return WaitUntil(() => hostMatch.IsUpgradePending, 8f);
                Assert.That(hostMatch.IsUpgradePending, Is.True, "No upgrade choice after clearing the room.");
                hostMatch.ChooseUpgrade(0);
                yield return WaitUntil(() => hostMatch.CurrentRoomIndex == 1, 8f);
                yield return WaitUntil(() => clientMatch.CurrentRoomIndex == 1, 8f);
                Assert.That(clientMatch.CurrentRoomIndex, Is.EqualTo(1), "Room advance did not replicate to the client.");
            }
            finally
            {
                Cleanup();
                LogAssert.ignoreFailingMessages = false;
            }
            yield return new WaitForSeconds(1.0f);
        }
    }
}
