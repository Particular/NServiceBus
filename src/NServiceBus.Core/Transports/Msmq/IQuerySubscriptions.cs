namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides means for MSMQ transport to query the subscription store.
    /// </summary>
    public interface IQuerySubscriptions
    {
        /// <summary>
        /// Returns the subcribers for a given set of types.
        /// </summary>
        /// <param name="eventTypes">A collection of types representing a message.</param>
        Task<IEnumerable<Subscriber>> GetSubscribersFor(IEnumerable<Type> eventTypes);
    }
}