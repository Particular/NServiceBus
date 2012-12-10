namespace Server
{
    using NServiceBus;

    public class EndpointConfig: IConfigureThisEndpoint, AsA_Publisher, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .CastleWindsorBuilder()
                .FileShareDataBus(@"..\..\..\DataBusShare\");
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
