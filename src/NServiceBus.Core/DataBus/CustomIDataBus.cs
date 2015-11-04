namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;

    class CustomIDataBus : Feature
    {
        public CustomIDataBus()
        {
            DependsOn<DataBus>();
        }

        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(context.Settings.Get<Type>("CustomDataBusType"), DependencyLifecycle.SingleInstance);

            return FeatureStartupTask.None;
        }
    }
}
