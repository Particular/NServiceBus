namespace NServiceBus.AutomaticSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Unicast;
    using Unicast.Routing;

    /// <summary>
    /// The default strategy for auto subscriptions.
    /// </summary>
    class AutoSubscriptionStrategy
    {
        /// <summary>
        /// The known handlers
        /// </summary>
        public MessageHandlerRegistry HandlerRegistry { get; set; }

        /// <summary>
        /// The message routing
        /// </summary>
        public StaticMessageRouter MessageRouter { get; set; }

        public Conventions Conventions { get; set; }

        /// <summary>
        /// If set to true the endpoint will subscribe to it self even if no endpoint mappings exists
        /// </summary>
        public bool DoNotRequireExplicitRouting { get; set; }

        /// <summary>
        /// if true messages that are handled by sagas wont be auto subscribed
        /// </summary>
        public bool DoNotAutoSubscribeSagas { get; set; }

        /// <summary>
        /// If true all messages that are not commands will be auto subscribed
        /// </summary>
        public bool SubscribePlainMessages { get; set; }

        public IEnumerable<Type> GetEventsToSubscribe()
        {
            return HandlerRegistry.GetMessageTypes()
                //get all potential messages
                .Where(t => !Conventions.IsCommandType(t) && (SubscribePlainMessages || Conventions.IsEventType(t)))
                //get messages that has routing if required
                .Where(t => DoNotRequireExplicitRouting || MessageRouter.GetDestinationFor(t).Any())
                //get messages with other handlers than sagas if needed
                .Where(t => !DoNotAutoSubscribeSagas || HandlerRegistry.GetHandlerTypes(t).Any(handler => !typeof(Saga.Saga).IsAssignableFrom(handler)))
                .ToList();
        }
    }


    /// <summary>
    /// Abstracts the strategy for selecting which events to auto-subscribe to during startup
    /// </summary>
    [ObsoleteEx(
      Message = "Not an extension point any more. If you want full control over autosubscribe please turn the feature off and implement your own for-loop calling Bus.Subscribe<YourEvent>() when starting your endpoint",
      RemoveInVersion = "6.0",
      TreatAsErrorFromVersion = "5.0")]
    public interface IAutoSubscriptionStrategy
    {
        /// <summary>
        /// Returns the list of events to auto-subscribe
        /// </summary>
        IEnumerable<Type> GetEventsToSubscribe();
    }

}