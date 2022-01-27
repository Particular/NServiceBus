namespace NServiceBus.Pipeline
{
    using Transport;

    /// <summary>
    /// Provide context to behaviors on the recoverability pipeline.
    /// </summary>
    public interface IRecoverabilityContext : IBehaviorContext
    {
        /// <summary>
        /// The message that failed processing.
        /// </summary>
        IncomingMessage FailedMessage { get; }

        /// <summary>
        /// The recoverability action to take for the failed message.
        /// </summary>
        ErrorHandleResult ActionToTake { get; set; }
    }
}