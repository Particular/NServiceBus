namespace NServiceBus.Unicast.Config
{
    using NServiceBus.Config;
    using Timeout;
    using Transports;

    public class DefaultToTimeoutManagerBasedDeferal:IFinalizeConfiguration
    {
        public void FinalizeConfiguration()
        {
            if (Configure.HasComponent<IDeferMessages>())
                return;

            Configure.Component<TimeoutManagerDeferrer>(DependencyLifecycle.InstancePerCall)
              .ConfigureProperty(p => p.TimeoutManagerAddress, Configure.Instance.GetTimeoutManagerAddress());
        }
    }
}