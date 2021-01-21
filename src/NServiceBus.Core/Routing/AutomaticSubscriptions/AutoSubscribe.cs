namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Unicast;
    using Unicast.Queuing;

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
            if (!context.Settings.TryGet(out SubscribeSettings settings))
            {
                settings = new SubscribeSettings();
            }

            var conventions = context.Settings.Get<Conventions>();

            context.RegisterStartupTask(b =>
            {
                var handlerRegistry = b.GetRequiredService<MessageHandlerRegistry>();
                var messageTypesHandled = GetMessageTypesHandledByThisEndpoint(handlerRegistry, conventions, settings);
                return new ApplySubscriptions(messageTypesHandled);
            });
        }

        static Type[] GetMessageTypesHandledByThisEndpoint(MessageHandlerRegistry handlerRegistry, Conventions conventions, SubscribeSettings settings)
        {
            var messageTypesHandled = handlerRegistry.GetMessageTypes() //get all potential messages
                .Where(t => !conventions.IsInSystemConventionList(t)) //never auto-subscribe system messages
                .Where(t => !conventions.IsCommandType(t)) //commands should never be subscribed to
                .Where(t => conventions.IsEventType(t)) //only events unless the user asked for all messages
                .Where(t => settings.AutoSubscribeSagas || handlerRegistry.GetHandlersFor(t).Any(handler => !typeof(Saga).IsAssignableFrom(handler.HandlerType))) //get messages with other handlers than sagas if needed
                .Except(settings.ExcludedTypes)
                .ToArray();

            return messageTypesHandled;
        }

        class ApplySubscriptions : FeatureStartupTask
        {
            public ApplySubscriptions(Type[] messagesHandledByThisEndpoint)
            {
                this.messagesHandledByThisEndpoint = messagesHandledByThisEndpoint;
            }

            protected override Task OnStart(IMessageSession session)
            {
                var tasks = new Task[messagesHandledByThisEndpoint.Length];
                for (var i = 0; i < messagesHandledByThisEndpoint.Length; i++)
                {
                    var eventType = messagesHandledByThisEndpoint[i];

                    tasks[i] = SubscribeToEvent(session, eventType);
                }
                return Task.WhenAll(tasks);
            }

            protected override Task OnStop(IMessageSession session)
            {
                return Task.CompletedTask;
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
                    Logger.Error($"AutoSubscribe was unable to subscribe to event '{eventType.FullName}': {e.Message}");
                    // swallow exception
                }
            }

            Type[] messagesHandledByThisEndpoint;
            static ILog Logger = LogManager.GetLogger<AutoSubscribe>();
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