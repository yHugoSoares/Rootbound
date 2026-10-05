using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Rootbound.Unity;
using Rootbound.Fusion;

namespace Rootbound.Tests
{
    // Local two-peer connection gate using Fusion Multi-Peer: a host and a client
    // NetworkRunner in the same process, over the real Photon transport.
    public class FusionConnectionGateTests
    {
        private const float ConnectTimeout = 45f;

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

        private static IEnumerator Shutdown(FusionNetworkSession session)
        {
            if (session == null) yield break;
            session.Leave();
            UnityEngine.Object.Destroy(session.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HostCreatesCodeClientJoinsAndBothLeaveCleanly()
        {
            FusionNetworkSession host = CreateSession("GateHost");
            yield return null;

            host.StartHost();
            yield return WaitUntil(() => host.State == NetworkSessionState.Connected, ConnectTimeout);
            Assert.That(host.State, Is.EqualTo(NetworkSessionState.Connected), "Host did not connect: " + host.LastError);
            Assert.That(host.SessionCode, Is.Not.Empty, "Host did not produce a session code.");

            FusionNetworkSession client = CreateSession("GateClient");
            yield return null;
            client.Join(host.SessionCode);
            yield return WaitUntil(() => client.State == NetworkSessionState.Connected, ConnectTimeout);
            Assert.That(client.State, Is.EqualTo(NetworkSessionState.Connected), "Client did not connect: " + client.LastError);

            yield return WaitUntil(() => host.PlayerCount >= 2, 8f);
            Assert.That(host.PlayerCount, Is.GreaterThanOrEqualTo(2), "Host did not observe the second peer.");

            client.Leave();
            yield return WaitUntil(() => host.PlayerCount <= 1, 8f);
            Assert.That(host.PlayerCount, Is.LessThanOrEqualTo(1), "Host did not observe the client leaving.");

            host.Leave();
            Assert.That(host.State, Is.EqualTo(NetworkSessionState.Offline));

            yield return Shutdown(host);
            yield return Shutdown(client);
        }

        [UnityTest]
        public IEnumerator FreshHostJoinFailedJoinAndSoloFallback()
        {
            // Fusion logs an [Error] on the intentional failed join; do not treat
            // that expected log as a test failure.
            LogAssert.ignoreFailingMessages = true;
            try
            {
                yield return RunFreshHostJoinFailedJoinAndSoloFallback();
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        private IEnumerator RunFreshHostJoinFailedJoinAndSoloFallback()
        {
            // First host/join.
            FusionNetworkSession hostA = CreateSession("GateHostA");
            yield return null;
            hostA.StartHost();
            yield return WaitUntil(() => hostA.State == NetworkSessionState.Connected, ConnectTimeout);
            Assert.That(hostA.State, Is.EqualTo(NetworkSessionState.Connected), "First host failed: " + hostA.LastError);
            yield return new WaitForSeconds(1.0f);

            FusionNetworkSession clientA = CreateSession("GateClientA");
            yield return null;
            clientA.Join(hostA.SessionCode);
            yield return WaitUntil(() => clientA.State == NetworkSessionState.Connected, ConnectTimeout);
            Assert.That(clientA.State, Is.EqualTo(NetworkSessionState.Connected), "First client join failed: " + clientA.LastError);
            clientA.Leave();
            hostA.Leave();
            yield return Shutdown(clientA);
            yield return Shutdown(hostA);
            yield return new WaitForSeconds(1.0f);

            // Fresh host/join after full shutdown.
            FusionNetworkSession hostB = CreateSession("GateHostB");
            yield return null;
            hostB.StartHost();
            yield return WaitUntil(() => hostB.State == NetworkSessionState.Connected, ConnectTimeout);
            Assert.That(hostB.State, Is.EqualTo(NetworkSessionState.Connected), "Fresh host after shutdown failed: " + hostB.LastError);
            yield return new WaitForSeconds(2.0f);

            FusionNetworkSession clientB = CreateSession("GateClientB");
            yield return null;
            clientB.Join(hostB.SessionCode);
            yield return WaitUntil(() => clientB.State == NetworkSessionState.Connected, ConnectTimeout);
            Assert.That(clientB.State, Is.EqualTo(NetworkSessionState.Connected), "Fresh client join failed: " + clientB.LastError);
            clientB.Leave();
            hostB.Leave();
            yield return Shutdown(clientB);
            yield return Shutdown(hostB);
            yield return new WaitForSeconds(0.5f);

            // Failed join must not hang and must leave the menu usable.
            FusionNetworkSession bad = CreateSession("GateBadClient");
            yield return null;
            bad.Join("RB-ZZZZ");
            yield return WaitUntil(() => bad.State == NetworkSessionState.Error || bad.State == NetworkSessionState.Offline, 30f);
            Assert.That(bad.State, Is.EqualTo(NetworkSessionState.Error), "Failed join did not return an error state.");
            yield return Shutdown(bad);

            // Solo remains available after every online failure/exit.
            OfflineNetworkSession solo = new OfflineNetworkSession();
            solo.StartHost();
            Assert.That(solo.State, Is.EqualTo(NetworkSessionState.Hosting), "Solo unavailable after failed join.");
        }
    }
}
