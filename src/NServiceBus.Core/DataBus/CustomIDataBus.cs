namespace NServiceBus.Features
{
    using System;

    class CustomIDataBus : Feature
    {
        public CustomIDataBus()
        {
            DependsOn<DataBus>();

            EnableByDefault();

            Prerequisite(context => UseDataBusExtensions.ShouldDataBusFeatureBeEnabled(this, context), "CustomDataBusImplementation not enabled since databus definition not detected.");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(context.Settings.Get<Type>("CustomDataBusType"), DependencyLifecycle.SingleInstance);
        }
    }
}