namespace NServiceBus.Timeout.Hosting.Windows.Config
{
    using System;
    using Core;
    using Settings;

    public class TimeoutManagerDefaults : IWantToRunBeforeConfigurationIsFinalized
    {
        private static readonly Action DefaultPersistence = () => Configure.Instance.UseRavenTimeoutPersister();

        public void Run()
        {
            if (!Configure.Instance.IsTimeoutManagerEnabled())
            {
                return;
            }

            if (!Configure.Instance.Configurer.HasComponent<IPersistTimeouts>() && !SettingsHolder.Get<bool>("Endpoint.SendOnly"))
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