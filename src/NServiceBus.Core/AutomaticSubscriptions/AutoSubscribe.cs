namespace NServiceBus.Features
{
    using AutomaticSubscriptions;
    using Config;
    using Settings;
    using Transports;

    public class AutoSubscribe : Feature
    {
        public override void Initialize(Configure config)
        {
            InfrastructureServices.Enable<IAutoSubscriptionStrategy>();

            if (config.Configurer.HasComponent<DefaultAutoSubscriptionStrategy>())
            {
                var transportDefinition = SettingsHolder.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport");

                //if the transport has centralized pubsub we can auto-subscribe all events regardless if they have explicit routing or not
                if (transportDefinition != null && transportDefinition.HasSupportForCentralizedPubSub)
                {
                    config.Configurer.ConfigureProperty<DefaultAutoSubscriptionStrategy>(s => s.DoNotRequireExplicitRouting, true);
                }
                
                //apply any user specific settings
                SettingsHolder.ApplyTo<DefaultAutoSubscriptionStrategy>();
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