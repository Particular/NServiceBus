namespace NServiceBus.Transports
{
    public class DefaultTransportForHost : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {

            if (Configure.Instance.Configurer.HasComponent<ISendMessages>())
            {
                return;
            }

            Configure.Instance.UseTransport<NServiceBus.Msmq>();
        }
    }
}