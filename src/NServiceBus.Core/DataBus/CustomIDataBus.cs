namespace NServiceBus.Features
{
    using NServiceBus.DataBus;

    class CustomIDataBus : Feature
    {
        public CustomIDataBus()
        {
            DependsOn<DataBus>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var customDataBusDefinition = context.Settings.Get<DataBusDefinition>(DataBus.SelectedDataBusKey) as CustomDataBus;

            context.Container.ConfigureComponent(customDataBusDefinition.DataBusType, DependencyLifecycle.SingleInstance);
        }
    }
}