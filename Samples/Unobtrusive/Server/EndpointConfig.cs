namespace Server
{
    using NServiceBus;

    public class EndpointConfig: IConfigureThisEndpoint, AsA_Publisher, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .CastleWindsorBuilder()
                .DefiningCommandsAs(t => t.Namespace != null && t.Namespace.EndsWith("Commands"))
                .DefiningEventsAs(t => t.Namespace != null && t.Namespace.EndsWith("Events"))
                .DefiningMessagesAs(t => t.Namespace != null && t.Namespace == "Messages")
                .DefiningEncryptedPropertiesAs(p => p.Name.StartsWith("Encrypted"));
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
