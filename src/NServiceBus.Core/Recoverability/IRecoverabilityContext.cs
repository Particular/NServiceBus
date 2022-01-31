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
        /// The recoverability action to take for this message.
        /// </summary>
        RecoverabilityAction RecoverabilityAction { get; set; }

        /// <summary>
        /// Locks the recoverability action for further changes.
        /// </summary>
        void PreventChanges();
    }
}