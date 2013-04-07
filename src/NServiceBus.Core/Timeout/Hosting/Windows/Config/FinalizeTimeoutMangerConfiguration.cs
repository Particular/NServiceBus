namespace NServiceBus.Timeout.Hosting.Windows.Config
{
    using System;
    using Core;
    using NServiceBus.Config;
    using Settings;
    using Transports;

    public class FinalizeTimeoutMangerConfiguration : IFinalizeConfiguration
    {
        public void FinalizeConfiguration()
        {
            if (!Configure.Instance.IsTimeoutManagerEnabled())
            {
                return;
            }

            if (Configure.Instance.GetMasterNodeAddress() != Address.Local ||//if we have a master node configured we should use that timeout manager instead
                Configure.Instance.Configurer.HasComponent<IDeferMessages>() || //if there is another deferral mechanism registered
                SettingsHolder.Get<bool>("Endpoint.SendOnly")) //send only endpoints doesn't need a TM
            {
                Configure.Instance.DisableTimeoutManager();
                return;
            }


            Configure.Component<TimeoutManagerBasedDeferral>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.TimeoutManagerAddress, Configure.Instance.GetTimeoutManagerAddress());
            
            Configure.Component<TimeoutPersisterReceiver>(DependencyLifecycle.SingleInstance);

            Infrastructure.Enable<IPersistTimeouts>();
            Infrastructure.Enable<IManageTimeouts>();
        }
    }
}