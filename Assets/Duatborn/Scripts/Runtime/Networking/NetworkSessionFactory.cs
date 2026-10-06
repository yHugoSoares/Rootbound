using System;

namespace Duatborn.Unity
{
    public static class NetworkSessionFactory
    {
        public static Func<INetworkSession> Create;

        public static INetworkSession New()
        {
            if (Create != null)
            {
                INetworkSession session = Create();
                if (session != null) return session;
            }
            return new OfflineNetworkSession();
        }
    }
}
