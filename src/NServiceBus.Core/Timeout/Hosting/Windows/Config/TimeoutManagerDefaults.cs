namespace NServiceBus.Timeout.Hosting.Windows.Config
{
    using System;
    using Core;

    public class TimeoutManagerDefaults : IWantToRunBeforeConfigurationIsFinalized
    {
        private static readonly Action DefaultPersistence = () => Configure.Instance.UseRavenTimeoutPersister();

        public void Run()
        {
            if (!Configure.Instance.IsTimeoutManagerEnabled())
            {
                return;
            }

            if (!Configure.Instance.Configurer.HasComponent<IPersistTimeouts>() && !Configure.Endpoint.IsSendOnly)
            {
                DefaultPersistence();
            }

            Configure.Instance.Configurer.ConfigureComponent<TimeoutPersisterReceiver>(DependencyLifecycle.SingleInstance);
            if (!Configure.Instance.Configurer.HasComponent<IManageTimeouts>())
            {
                Configure.Instance.Configurer.ConfigureComponent<DefaultTimeoutManager>(DependencyLifecycle.SingleInstance);
            }
        }
    }
}