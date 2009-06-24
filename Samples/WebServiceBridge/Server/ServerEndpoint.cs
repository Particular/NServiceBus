using NServiceBus;
using NServiceBus.Host;

namespace Server
{
    public class ServerEndpoint : IConfigureThisEndpoint
    {
        public void Init(Configure configure)
        {
            configure
                .MsmqSubscriptionStorage()
                .BinarySerializer()
                .MsmqTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false)
                    .LoadMessageHandlers();
        }
    }
}