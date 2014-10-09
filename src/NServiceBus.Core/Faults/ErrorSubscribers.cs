namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Faults;

    class ErrorSubscribers : Feature
    {
        public ErrorSubscribers()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var subscriberTypes = context.Settings.GetOrDefault<List<Type>>("ErrorSubscribers") ?? new List<Type>();

            foreach (var subscriberType in subscriberTypes)
            {
                context.Container.ConfigureComponent(subscriberType, DependencyLifecycle.SingleInstance);
            }

            context.Container.ConfigureComponent<ErrorSubscribersCoordinator>(DependencyLifecycle.SingleInstance);
        }
    }
}
