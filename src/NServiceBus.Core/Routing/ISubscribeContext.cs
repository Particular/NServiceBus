namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// Provides context for subscription requests.
    /// </summary>
    public partial interface ISubscribeContext : IBehaviorContext
    {
        /// <summary>
        /// The type of the events.
        /// </summary>
        Type[] EventTypes { get; }
    }
}