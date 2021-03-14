namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    class CustomIDataBus : Feature
    {
        public CustomIDataBus()
        {
            DependsOn<DataBus>();
        }

        protected internal override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default)
        {
            context.Container.ConfigureComponent(context.Settings.Get<Type>("CustomDataBusType"), DependencyLifecycle.SingleInstance);
            return Task.CompletedTask;
        }
    }
}