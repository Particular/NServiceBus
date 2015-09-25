namespace NServiceBus.Pipeline.Contexts
{
    using NServiceBus.Extensibility;

    /// <summary>
    /// Outgoing pipeline context.
    /// </summary>
    public class OutgoingContext : BehaviorContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="OutgoingContext"/>.
        /// </summary>
        /// <param name="parentContext">The parent context.</param>
        public OutgoingContext(ContextBag parentContext)
            : base(parentContext)
        {
        }
    }
}