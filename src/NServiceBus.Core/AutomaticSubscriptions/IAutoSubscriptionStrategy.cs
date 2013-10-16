namespace NServiceBus.AutomaticSubscriptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Abstracts the strategy for selecting which events to auto-subscribe to during startup
    /// </summary>
    public interface IAutoSubscriptionStrategy
    {
        /// <summary>
        /// Returns the list of events to auto-subscribe
        /// </summary>
        /// <returns></returns>
        IEnumerable<Type> GetEventsToSubscribe();
    }
}