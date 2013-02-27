namespace NServiceBus.Unicast.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Auto subscription strategy for transports (brokers) that has a centralized subscription mechanish and there by not requiring any config 
    /// </summary>
    public class NoConfigRequiredAutoSubscriptionStrategy : IAutoSubscriptionStrategy
    {
        /// <summary>
        /// The known handlers
        /// </summary>
        public IMessageHandlerRegistry HandlerRegistry { get; set; }

        public IEnumerable<Type> GetEventsToSubscribe()
        {
            return HandlerRegistry.GetMessageTypes()
              .Where(t => !MessageConventionExtensions.IsCommandType(t))
              .ToList();

        }
    }

}