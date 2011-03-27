namespace NServiceBus.Gateway
{
    //todo Remove when the master profiles are introduced
    public class EndpointConfig : IConfigureThisEndpoint, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With().Log4Net().DefaultBuilder().UnicastBus();
        }
    }
}
