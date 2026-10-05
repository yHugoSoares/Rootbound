using System;

namespace Rootbound.Unity
{
    public enum NetworkSessionState
    {
        Offline = 0,
        Starting = 1,
        Hosting = 2,
        Joining = 3,
        Connected = 4,
        Disconnected = 5,
        Error = 6
    }

    public interface INetworkSession
    {
        NetworkSessionState State { get; }
        bool IsHost { get; }
        string SessionCode { get; }
        string LastError { get; }
        Rootbound.Core.CreatureKind LocalCreature { get; set; }
        event Action Changed;

        void StartHost();
        void Join(string code);
        void Leave();
    }
}
