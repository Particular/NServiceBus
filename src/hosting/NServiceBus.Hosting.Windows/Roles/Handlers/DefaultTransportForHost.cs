namespace NServiceBus.Hosting.Windows.Roles.Handlers
{
    using NServiceBus.Unicast.Queuing;

    public class DefaultTransportForHost : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {

            if (Configure.Instance.Configurer.HasComponent<ISendMessages>())
            {
                return;
            }

            Configure.Instance.UseTransport<Msmq>();
        }
    }
}