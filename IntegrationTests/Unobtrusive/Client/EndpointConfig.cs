namespace Client
{
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Client, IWantCustomInitialization
    {
        public Configure Init()
        {
            var configure = Configure.With()
                .DefaultBuilder()
                .FileShareDataBus(@"..\..\..\DataBusShare\")
                .RijndaelEncryptionService();
            return configure;
        }
    }

}
