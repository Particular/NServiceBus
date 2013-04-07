namespace NServiceBus.Timeout.Hosting.Windows.Config
{
    using Core;
    using NServiceBus.Config;

    public class TimeoutManagerDefaults : IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            Infrastructure.SetDefaultFor<IManageTimeouts>(typeof(DefaultTimeoutManager), DependencyLifecycle.SingleInstance);
        }
    }
}