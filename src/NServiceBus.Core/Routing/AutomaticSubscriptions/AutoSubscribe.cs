namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Logging;
    using Routing.MessageDrivenSubscriptions;
    using Transport;
    using Unicast;

    /// <summary>
    /// Used to configure auto subscriptions.
    /// </summary>
    public class AutoSubscribe : Feature
    {
        internal AutoSubscribe()
        {
            EnableByDefault();
            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't autosubscribe.");
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            SubscribeSettings settings;

            if (!context.Settings.TryGet(out settings))
            {
                settings = new SubscribeSettings();
            }

            var conventions = context.Settings.Get<Conventions>();
            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();
            var requireExplicitRouting = transportInfrastructure.OutboundRoutingPolicy.Publishes == OutboundRoutingType.Unicast;
            var publishers = context.Settings.Get<Publishers>();

            context.RegisterStartupTask(b =>
            {
                var handlerRegistry = b.Build<MessageHandlerRegistry>();
                var messageTypesHandled = GetMessageTypesHandledByThisEndpoint(handlerRegistry, conventions, settings);
                var typesToSubscribe = messageTypesHandled.Where(eventType => !requireExplicitRouting || publishers.GetPublisherFor(eventType).Any());
                return new ApplySubscriptions(typesToSubscribe);
            });
        }

        static List<Type> GetMessageTypesHandledByThisEndpoint(MessageHandlerRegistry handlerRegistry, Conventions conventions, SubscribeSettings settings)
        {
            var messageTypesHandled = handlerRegistry.GetMessageTypes() //get all potential messages
                .Where(t => !conventions.IsInSystemConventionList(t)) //never auto-subscribe system messages
                .Where(t => !conventions.IsCommandType(t)) //commands should never be subscribed to
                .Where(conventions.IsEventType) //only events unless the user asked for all messages
                .Where(t => settings.AutoSubscribeSagas || handlerRegistry.GetHandlersFor(t).Any(handler => !typeof(Saga).IsAssignableFrom(handler.HandlerType))) //get messages with other handlers than sagas if needed
                .ToList();

            return messageTypesHandled;
        }

        class ApplySubscriptions : FeatureStartupTask
        {
            public ApplySubscriptions(IEnumerable<Type> messagesHandledByThisEndpoint)
            {
                this.messagesHandledByThisEndpoint = messagesHandledByThisEndpoint;
            }

            protected override async Task OnStart(IMessageSession session)
            {
                foreach (var eventType in messagesHandledByThisEndpoint)
                {
                    await session.Subscribe(eventType).ConfigureAwait(false);
                    Logger.DebugFormat("Auto subscribed to event {0}", eventType);
                }
            }

            protected override Task OnStop(IMessageSession session)
            {
                return TaskEx.CompletedTask;
            }

            IEnumerable<Type> messagesHandledByThisEndpoint;
            static ILog Logger = LogManager.GetLogger<ApplySubscriptions>();
        }

        internal class SubscribeSettings
        {
            public SubscribeSettings()
            {
                AutoSubscribeSagas = true;
            }

            public bool AutoSubscribeSagas { get; set; }
        }
    }
}