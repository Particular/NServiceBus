using System;

namespace Server
{
    using NServiceBus;

    public class EndpointConfig: IConfigureThisEndpoint, AsA_Publisher, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
            .DefaultBuilder()
            .FileShareDataBus(@"..\..\..\databus\")
            .DefiningCommandsAs(t => t.Namespace != null && t.Namespace.EndsWith("Commands"))
            .DefiningEventsAs(t => t.Namespace != null && t.Namespace.EndsWith("Events"))
            .DefiningMessagesAs(t => t.Namespace != null && t.Namespace == "Messages")
            .DefiningEncryptedPropertiesAs(p => p.Name.StartsWith("Encrypted"))
            .DefiningDataBusPropertiesAs(p => p.Name.EndsWith("DataBus"))
            .DefiningExpressMessagesAs(t => t.Name.EndsWith("Express"))
            .DefiningTimeToBeReceivedAs(t =>
            {
                if (t.Name.EndsWith("Expires"))
                {
                    return TimeSpan.FromMinutes(3);
                }

                return TimeSpan.MaxValue;
            });
        }
    }

    class EncryptionConfig : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.RijndaelEncryptionService();
        }
    }
}
