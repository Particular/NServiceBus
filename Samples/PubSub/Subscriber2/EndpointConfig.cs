using NServiceBus;
using NServiceBus.Host;

namespace Subscriber2
{
    class EndpointConfig : IConfigureThisEndpoint
    {
        public void Init(Configure configure)
        {
            configure
                .XmlSerializer()
                .MsmqTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false)
                    .LoadMessageHandlers();
        }
    }
}
