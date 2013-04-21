namespace NServiceBus.Unicast.Config
{
    using System;
    using Settings;
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

            if(SettingsHolder.GetOrDefault<Type>("NServiceBus.Transport.SelectedTransport") != null)
            {
                return;
            }

            Configure.Instance.UseTransport<Msmq>();
        }
    }
}