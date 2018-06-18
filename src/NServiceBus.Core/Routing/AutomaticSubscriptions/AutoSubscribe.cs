namespace NServiceBus.Features
{
    using Logging;
    using NServiceBus.Unicast.Queuing;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
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
            var publishers = context.Routing.Publishers;

            context.RegisterStartupTask(b =>
            {
                var handlerRegistry = b.Build<MessageHandlerRegistry>();
                var messageTypesHandled = GetMessageTypesHandledByThisEndpoint(handlerRegistry, conventions, settings);
                var typesToSubscribe = messageTypesHandled.Where(eventType => !requireExplicitRouting || publishers.GetPublisherFor(eventType).Any()).ToList();
                return new ApplySubscriptions(typesToSubscribe, settings.ExcludedTypes);
            });
        }

        static List<Type> GetMessageTypesHandledByThisEndpoint(MessageHandlerRegistry handlerRegistry, Conventions conventions, SubscribeSettings settings)
        {
            var messageTypesHandled = handlerRegistry.GetMessageTypes() //get all potential messages
                .Where(t => !conventions.IsInSystemConventionList(t)) //never auto-subscribe system messages
                .Where(t => !conventions.IsCommandType(t)) //commands should never be subscribed to
                .Where(t => conventions.IsEventType(t)) //only events unless the user asked for all messages
                .Where(t => settings.AutoSubscribeSagas || handlerRegistry.GetHandlersFor(t).Any(handler => !typeof(Saga).IsAssignableFrom(handler.HandlerType))) //get messages with other handlers than sagas if needed
                .ToList();

            return messageTypesHandled;
        }

        class ApplySubscriptions : FeatureStartupTask
        {
            public ApplySubscriptions(List<Type> messagesHandledByThisEndpoint, HashSet<Type> excludedTypes)
            {
                this.messagesHandledByThisEndpoint = messagesHandledByThisEndpoint;
                this.excludedTypes = excludedTypes;
            }

            protected override Task OnStart(IMessageSession session)
            {
                var tasks = new Task[messagesHandledByThisEndpoint.Count];
                for (var i = 0; i < messagesHandledByThisEndpoint.Count; i++)
                {
                    var eventType = messagesHandledByThisEndpoint[i];
                    tasks[i] = excludedTypes.Contains(eventType)
                        ? TaskEx.CompletedTask
                        : SubscribeToEvent(session, eventType);
                }
                return Task.WhenAll(tasks);
            }

            protected override Task OnStop(IMessageSession session)
            {
                return TaskEx.CompletedTask;
            }

            static async Task SubscribeToEvent(IMessageSession session, Type eventType)
            {
                try
                {
                    await session.Subscribe(eventType).ConfigureAwait(false);
                    Logger.DebugFormat("Auto subscribed to event {0}", eventType);
                }
                catch (Exception e) when (!(e is QueueNotFoundException))
                {
                    Logger.Warn($"AutoSubscribe was unable to subscribe to event '{eventType.FullName}': {e.Message}");
                    // swallow exception
                }
            }

            List<Type> messagesHandledByThisEndpoint;
            HashSet<Type> excludedTypes;
            static ILog Logger = LogManager.GetLogger<ApplySubscriptions>();
        }

        internal class SubscribeSettings
        {
            public SubscribeSettings()
            {
                AutoSubscribeSagas = true;
            }

            public bool AutoSubscribeSagas { get; set; }
            public HashSet<Type> ExcludedTypes { get; set; } = new HashSet<Type>();
        }
    }
}