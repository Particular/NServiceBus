namespace NServiceBus.Unicast.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;
    using Routing;
    using Saga;

    /// <summary>
    /// The default strategy for auto subscriptions.
    /// </summary>
    public class DefaultAutoSubscriptionStrategy:IAutoSubscriptionStrategy
    {
        /// <summary>
        /// The known handlers
        /// </summary>
        public IMessageHandlerRegistry HandlerRegistry { get; set; }

        /// <summary>
        /// The message routing
        /// </summary>
        public IRouteMessages MessageRouter { get; set; }

        /// <summary>
        /// If set to true the endpoint will subscribe to it self even if no endpoint mappings exists
        /// </summary>
        public bool AllowSubscribeToSelf { get; set; }


        /// <summary>
        /// if true messages that are handled by sagas wont be auto subscribed
        /// </summary>
        public bool DoNotAutoSubscribeSagas { get; set; }

        public IEnumerable<Type> GetEventsToSubscribe()
        {
            var eventTypes = GetEventsToAutoSubscribe();
            foreach (var eventType in eventTypes)
            {
                var otherHandlersThanSagas = HandlerRegistry.GetHandlerTypes(eventType).Any(t => !typeof(ISaga).IsAssignableFrom(t));

                if (DoNotAutoSubscribeSagas && !otherHandlersThanSagas)
                {
                    Log.InfoFormat("Message type {0} is not auto subscribed since its only handled by sagas and auto subscription for sagas is currently turned off", eventType);
                    continue;
                }

                if (!MessageConventionExtensions.IsEventType(eventType))
                    Log.Info("Future versions of NServiceBus will only auto-subscribe messages explicitly marked as IEvent so consider marking messages that are events with the explicit IEvent interface");

                yield return eventType;
            }
        }

       
        IEnumerable<Type> GetEventsToAutoSubscribe()
        {
            var eventsHandled = HandlerRegistry.GetMessageTypes()
                .Where(t => !MessageConventionExtensions.IsCommandType(t))
                .ToList();

            if (AllowSubscribeToSelf)
            {
                return eventsHandled;
            }

            var eventsWithRouting = eventsHandled.Where(e => MessageRouter.GetDestinationFor(e) != Address.Undefined).ToList();

            return eventsWithRouting;
        }


        readonly static ILog Log = LogManager.GetLogger(typeof(DefaultAutoSubscriptionStrategy));
    }

}