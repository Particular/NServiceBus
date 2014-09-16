namespace Client
{
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Client
    {
        public void Customize(BusConfiguration configuration)
        {
            configuration.UsePersistence<InMemoryPersistence>();
            configuration.FileShareDataBus(@"..\..\..\DataBusShare\");
            configuration.RijndaelEncryptionService();
        }
    }

}
