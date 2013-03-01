namespace NServiceBus.Timeout.Hosting.Windows.Config
{
    using System;
    using Core;
    using Settings;
    using Transports;

    public class TimeoutManagerDefaults : IWantToRunBeforeConfigurationIsFinalized
    {
        private static readonly Action DefaultPersistence = () => Configure.Instance.UseRavenTimeoutPersister();

        public void Run()
        {
            if (!Configure.Instance.IsTimeoutManagerEnabled())
            {
                return;
            }

            
            if (Configure.Instance.GetMasterNodeAddress() != Address.Local ||//if we have a master node configured we should use that timeout manager instead
                Configure.Instance.Configurer.HasComponent<IDeferMessages>()) //if there is another deferral mechanism registered
            {
                Configure.Instance.DisableTimeoutManager();
                return;
            }


            Configure.Instance.Configurer.ConfigureComponent<TimeoutManagerBasedDeferral>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.TimeoutManagerAddress, Configure.Instance.GetTimeoutManagerAddress());


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