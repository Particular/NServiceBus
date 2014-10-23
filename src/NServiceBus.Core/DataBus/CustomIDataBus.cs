namespace NServiceBus.Features
{
    using System;

    class CustomIDataBus : Feature
    {
        public CustomIDataBus()
        {
            DependsOn<DataBus>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(context.Settings.Get<Type>("CustomDataBusType"), DependencyLifecycle.SingleInstance);
        }
    }
}
