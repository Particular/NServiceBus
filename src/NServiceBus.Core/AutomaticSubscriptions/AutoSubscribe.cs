namespace NServiceBus.Features
{
    using AutomaticSubscriptions;
    using Transports;

    public class AutoSubscribe : Feature
    {
        public override void Initialize(Configure config)
        {

            config.Configurer.ConfigureComponent<AutoSubscriptionStrategy>(DependencyLifecycle.InstancePerCall);

            var transportDefinition = config.Settings.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport");

            //if the transport has centralized pubsub we can auto-subscribe all events regardless if they have explicit routing or not
            if (transportDefinition != null && transportDefinition.HasSupportForCentralizedPubSub)
            {
                config.Configurer.ConfigureProperty<AutoSubscriptionStrategy>(s => s.DoNotRequireExplicitRouting, true);
            }

            //apply any user specific settings
            var targetType = typeof(AutoSubscriptionStrategy);

            foreach (var property in targetType.GetProperties())
            {
                var settingsKey = targetType.FullName + "." + property.Name;

                if (config.Settings.HasSetting(settingsKey))
                {
                    config.Configurer.ConfigureProperty<AutoSubscriptionStrategy>(property.Name, config.Settings.Get(settingsKey));
                }
            }
        }

        public override bool IsEnabledByDefault
        {
            get
            {
                return true;
            }
        }
    }
}