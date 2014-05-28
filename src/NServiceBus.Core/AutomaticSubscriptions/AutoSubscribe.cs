namespace NServiceBus.Features
{
    using AutomaticSubscriptions;
    using Transports;

    public class AutoSubscribe : Feature
    {
        public AutoSubscribe()
        {
            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {

            context.Container.ConfigureComponent<AutoSubscriptionStrategy>(DependencyLifecycle.InstancePerCall);

            var transportDefinition = context.Settings.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport");

            //if the transport has centralized pubsub we can auto-subscribe all events regardless if they have explicit routing or not
            if (transportDefinition != null && transportDefinition.HasSupportForCentralizedPubSub)
            {
                context.Container.ConfigureProperty<AutoSubscriptionStrategy>(s => s.DoNotRequireExplicitRouting, true);
            }

            context.Container.ConfigureComponent<AutoSubscriber>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(t => t.Enabled, true);

            //apply any user specific settings
            var targetType = typeof(AutoSubscriptionStrategy);

            foreach (var property in targetType.GetProperties())
            {
                var settingsKey = targetType.FullName + "." + property.Name;

                if (context.Settings.HasSetting(settingsKey))
                {
                    context.Container.ConfigureProperty<AutoSubscriptionStrategy>(property.Name, context.Settings.Get(settingsKey));
                }
            }
        }
    }
}