namespace NServiceBus.Features
{
    using System.Linq;
    using NServiceBus.AutomaticSubscriptions;
    using NServiceBus.Logging;
    using NServiceBus.Transports;

    /// <summary>
    /// Used to configure auto subscriptions.
    /// </summary>
    public class AutoSubscribe : Feature
    {
        internal AutoSubscribe()
        {
            EnableByDefault();
            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't autosubscribe.");
            RegisterStartupTask<ApplySubscriptions>();
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<AutoSubscriptionStrategy>(DependencyLifecycle.InstancePerCall);

            var transportDefinition = context.Settings.Get<TransportDefinition>();

            //if the transport has centralized pubsub we can auto-subscribe all events regardless if they have explicit routing or not
            if (transportDefinition != null && transportDefinition.HasSupportForCentralizedPubSub)
            {
                context.Container.ConfigureProperty<AutoSubscriptionStrategy>(s => s.DoNotRequireExplicitRouting, true);
            }

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

        class ApplySubscriptions : FeatureStartupTask
        {
            public AutoSubscriptionStrategy AutoSubscriptionStrategy { get; set; }

            public IBus Bus { get; set; }

            public Conventions Conventions { get; set; }

            protected override void OnStart()
            {
                foreach (var eventType in AutoSubscriptionStrategy.GetEventsToSubscribe()
                    .Where(t => !Conventions.IsInSystemConventionList(t))) //never auto-subscribe system messages
                {
                    Bus.Subscribe(eventType);

                    Logger.DebugFormat("Auto subscribed to event {0}", eventType);
                }
            }

            static ILog Logger = LogManager.GetLogger<ApplySubscriptions>();
        }
    }
}