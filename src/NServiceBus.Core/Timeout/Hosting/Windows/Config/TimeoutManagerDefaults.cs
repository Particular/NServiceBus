namespace NServiceBus.Timeout.Hosting.Windows.Config
{
    using Core;
    using NServiceBus.Config;

    public class TimeoutManagerDefaults : IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            InfrastructureServices.SetDefaultFor<IManageTimeouts>(typeof(DefaultTimeoutManager), DependencyLifecycle.SingleInstance);
        }
    }
}