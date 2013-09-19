namespace Client
{
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Client, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                     .NinjectBuilder()
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
