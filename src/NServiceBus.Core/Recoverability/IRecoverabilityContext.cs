namespace NServiceBus.Pipeline
{
    using Transport;

    /// <summary>
    /// Provide context to behaviors on the recoverability pipeline.
    /// </summary>
    public interface IRecoverabilityContext : IBehaviorContext
    {
        /// <summary>
        /// Context for the message that failed processing.
        /// </summary>
        ErrorContext ErrorContext { get; }

        /// <summary>
        /// The recoverability action to take for the failed message.
        /// </summary>
        ErrorHandleResult ActionToTake { get; set; }
    }
}