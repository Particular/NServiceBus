namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;

    /// <summary>
    ///     Used to configure auto subscriptions.
    /// </summary>
    public class AutoSubscribe : Feature
    {
        internal AutoSubscribe()
        {
            EnableByDefault();
            Prerequisite(context => !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send only endpoints can't autosubscribe.");
        }

        /// <summary>
        ///     See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transportDefinition = context.Settings.Get<TransportDefinition>();
            SubscribeSettings settings;

            if (!context.Settings.TryGet(out settings))
            {
                settings = new SubscribeSettings();
            }

            var conventions = context.Settings.Get<Conventions>();

            if (transportDefinition.GetOutboundRoutingPolicy(context.Settings).Publishes == OutboundRoutingType.Multicast)
            {
                context.RegisterStartupTask(b =>
                {
                    var handlerRegistry = b.Build<MessageHandlerRegistry>();

                    var messageTypesHandled = GetMessageTypesHandledByThisEndpoint(handlerRegistry, conventions, settings);

                    return new ApplySubscriptions(messageTypesHandled, type => TaskEx.TrueTask);
                });
            }
            else
            {
                context.RegisterStartupTask(b =>
                {
                    var handlerRegistry = b.Build<MessageHandlerRegistry>();

                    var messageTypesToSubscribe = GetMessageTypesHandledByThisEndpoint(handlerRegistry, conventions, settings);

                    Func<Type, Task<bool>> asyncPredicate;
                    if (settings.RequireExplicitRouting)
                    {
                        var subscriptionRouter = b.Build<SubscriptionRouter>();

                        asyncPredicate = async type =>
                        {
                            var addresses = await subscriptionRouter.GetAddressesForEventType(type).ConfigureAwait(false);
                            return addresses.Any();
                        };
                    }
                    else
                    {
                        asyncPredicate = type => TaskEx.TrueTask;
                    }

                    return new ApplySubscriptions(messageTypesToSubscribe, asyncPredicate);
                });
            }
        }

        static List<Type> GetMessageTypesHandledByThisEndpoint(MessageHandlerRegistry handlerRegistry, Conventions conventions, SubscribeSettings settings)
        {
            var messageTypesHandled = handlerRegistry.GetMessageTypes() //get all potential messages
                .Where(t => !conventions.IsInSystemConventionList(t)) //never auto-subscribe system messages
                .Where(t => !conventions.IsCommandType(t)) //commands should never be subscribed to
                .Where(t => settings.SubscribePlainMessages || conventions.IsEventType(t)) //only events unless the user asked for all messages
                .Where(t => settings.AutoSubscribeSagas || handlerRegistry.GetHandlersFor(t).Any(handler => !typeof(Saga).IsAssignableFrom(handler.HandlerType))) //get messages with other handlers than sagas if needed
                .ToList();

            return messageTypesHandled;
        }

        class ApplySubscriptions : FeatureStartupTask
        {
            public ApplySubscriptions(IEnumerable<Type> messagesHandledByThisEndpoint, Func<Type, Task<bool>> asyncPredicate)
            {
                this.messagesHandledByThisEndpoint = messagesHandledByThisEndpoint;
                this.asyncPredicate = asyncPredicate;
            }

            protected override async Task OnStart(IBusSession session)
            {
                foreach (var eventType in messagesHandledByThisEndpoint)
                {
                    if (await asyncPredicate(eventType).ConfigureAwait(false))
                    {
                        await session.Subscribe(eventType).ConfigureAwait(false);
                        Logger.DebugFormat("Auto subscribed to event {0}", eventType);
                    }
                }
            }

            protected override Task OnStop(IBusSession session)
            {
                return TaskEx.CompletedTask;
            }

            Func<Type, Task<bool>> asyncPredicate;

            IEnumerable<Type> messagesHandledByThisEndpoint;

            static ILog Logger = LogManager.GetLogger<ApplySubscriptions>();
        }

        internal class SubscribeSettings
        {
            public SubscribeSettings()
            {
                AutoSubscribeSagas = true;
                RequireExplicitRouting = true;
            }

            public bool AutoSubscribeSagas { get; set; }
            public bool RequireExplicitRouting { get; set; }
            public bool SubscribePlainMessages { get; set; }
        }
    }
}