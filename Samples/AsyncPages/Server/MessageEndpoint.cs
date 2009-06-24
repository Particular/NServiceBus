using NServiceBus;
using NServiceBus.Host;

namespace Server
{
    public class MessageEndpoint : IConfigureThisEndpoint
    {
        public void Init(Configure config)
        {
            config
                .XmlSerializer()
                .MsmqTransport()
                    .IsTransactional(false)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false)
                    .LoadMessageHandlers();
        }
    }

}