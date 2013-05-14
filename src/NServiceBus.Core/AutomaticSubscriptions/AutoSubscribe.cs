namespace NServiceBus.Features
{
    using AutomaticSubscriptions;
    using Config;
    using Settings;
    using Transports;

    public class AutoSubscribe : Feature
    {
        public override void Initialize()
        {
            InfrastructureServices.Enable<IAutoSubscriptionStrategy>();

            if (Configure.HasComponent<DefaultAutoSubscriptionStrategy>())
            {
                var transportDefiniton = SettingsHolder.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport");

                //if the transport has centralized pubsub we can autosubscribe all events regardless if they have explicit routing or not
                if (transportDefiniton != null && transportDefiniton.HasSupportForCentralizedPubSub)
                {
                    Configure.Instance.Configurer.ConfigureProperty<DefaultAutoSubscriptionStrategy>(s => s.DoNotRequireExplicitRouting, true);
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