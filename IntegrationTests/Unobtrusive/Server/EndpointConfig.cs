namespace Server
{
    using NServiceBus;

    public class EndpointConfig: IConfigureThisEndpoint, AsA_Publisher, IWantCustomInitialization
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
