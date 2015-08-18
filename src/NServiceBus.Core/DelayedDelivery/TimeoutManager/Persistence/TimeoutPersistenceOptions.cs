namespace NServiceBus.Timeout.Core
{
    using NServiceBus.Extensibility;

    /// <summary>
    /// Contains details about the to be persisted timeouts.
    /// </summary>
    public class TimeoutPersistenceOptions
    {
        /// <summary>
        /// Creates a new instance of the TimeoutPersistenceOptions class.
        /// </summary>
        /// <param name="context">The context.</param>
        public TimeoutPersistenceOptions(ReadOnlyContextBag context)
        {
            Context = context;
        }

        /// <summary>
        /// Access to the behavior context.
        /// </summary>
        public ReadOnlyContextBag Context { get; set; }
    }
}