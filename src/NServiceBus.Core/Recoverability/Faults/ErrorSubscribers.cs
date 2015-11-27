namespace NServiceBus.Features
{
    using System.Collections.Generic;

    class ErrorSubscribers : Feature
    {
        public ErrorSubscribers()
        {
            EnableByDefault();
        }

        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<BusNotifications>(DependencyLifecycle.SingleInstance);

            return FeatureStartupTask.None;
        }
    }
}
