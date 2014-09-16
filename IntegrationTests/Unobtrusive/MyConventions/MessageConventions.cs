using System;

namespace MyConventions
{
    using NServiceBus;

    public class MessageConventions : INeedInitialization
    {
        public void Customize(BusConfiguration configuration)
        {
            configuration.Conventions().DefiningCommandsAs(t => t.Namespace != null && t.Namespace.EndsWith("Commands"))
                .DefiningEventsAs(t => t.Namespace != null && t.Namespace.EndsWith("Events"))
                .DefiningMessagesAs(t => t.Namespace == "Messages")
                .DefiningEncryptedPropertiesAs(p => p.Name.StartsWith("Encrypted"))
                .DefiningDataBusPropertiesAs(p => p.Name.EndsWith("DataBus"))
                .DefiningExpressMessagesAs(t => t.Name.EndsWith("Express"))
                .DefiningTimeToBeReceivedAs(t => t.Name.EndsWith("Expires")
                    ? TimeSpan.FromSeconds(30)
                    : TimeSpan.MaxValue
                );
        }
    }
}
