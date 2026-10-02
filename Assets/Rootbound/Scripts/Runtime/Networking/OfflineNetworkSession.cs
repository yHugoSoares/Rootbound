using System;

namespace Rootbound.Unity
{
    public sealed class OfflineNetworkSession : INetworkSession
    {
        public NetworkSessionState State { get; private set; }
        public bool IsHost { get; private set; }
        public string SessionCode { get; private set; }
        public string LastError { get; private set; }

        public event Action Changed;

        public OfflineNetworkSession()
        {
            State = NetworkSessionState.Offline;
            SessionCode = "LOCAL";
            LastError = string.Empty;
        }

        public void StartHost()
        {
            IsHost = true;
            SessionCode = "LOCAL";
            LastError = string.Empty;
            State = NetworkSessionState.Hosting;
            OnChanged();
        }

        public void Join(string code)
        {
            IsHost = false;
            LastError = "Online join is unavailable: Photon Fusion 2 is not installed. See docs/HANDOFF.md.";
            State = NetworkSessionState.Error;
            OnChanged();
        }

        public void Leave()
        {
            IsHost = false;
            SessionCode = "LOCAL";
            State = NetworkSessionState.Offline;
            OnChanged();
        }

        private void OnChanged()
        {
            if (Changed != null) Changed();
        }
    }
}
