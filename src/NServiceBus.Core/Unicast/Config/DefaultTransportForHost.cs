namespace NServiceBus.Unicast.Config
{
    using Transports;

    /// <summary>
    /// Default to MSMQ transport if no other transport has been configured. This can be removed when we introduce the modules concept
    /// </summary>
    public class DefaultTransportForHost : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            if (Configure.Instance.Configurer.HasComponent<ISendMessages>())
            {
                return;
            }

            if(Configure.Instance.Settings.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport") != null)
            {
                return;
            }

            Configure.Instance.UseTransport<Msmq>();
        }
    }
}