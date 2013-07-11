namespace NServiceBus.Timeout.Core
{
    using Config;

    public class TimeoutManagerDefaults : IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            InfrastructureServices.SetDefaultFor<IManageTimeouts>(typeof(DefaultTimeoutManager), DependencyLifecycle.SingleInstance);
        }
    }
}