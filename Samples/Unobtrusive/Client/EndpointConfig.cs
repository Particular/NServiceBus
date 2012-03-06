namespace Client
{
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Client, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .DefiningCommandsAs(t => t.Namespace != null && t.Namespace.EndsWith("Commands"))
                .DefiningEventsAs(t => t.Namespace != null && t.Namespace.EndsWith("Events"))
                .DefiningMessagesAs(t => t.Namespace != null && t.Namespace == "Messages")
                .DefiningEncryptedPropertiesAs(p => p.Name.StartsWith("Encrypted"));
            
            /*  Currently NServiceBus.Production and NServiceBus.Time Profiles are defined as command line parameters, so the following two lines to enable the Time features are
             * redundant. If you running NServiceBus under your own host (hence cannot use NServiceBus profiles, to enable Time features, uncomment the following two lines:
                .DefaultBuilder()
                .RunTimeoutManager();
             * */
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
