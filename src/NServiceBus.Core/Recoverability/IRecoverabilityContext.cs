namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
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
        /// The recoverability configuration for the endpoint.
        /// </summary>
        public RecoverabilityConfig RecoverabilityConfiguration { get; }

        /// <summary>
        /// The recoverability action to take for this message.
        /// </summary>
        RecoverabilityAction RecoverabilityAction { get; set; }

        /// <summary>
        /// Metadata for this message.
        /// </summary>
        Dictionary<string, string> Metadata { get; }

        /// <summary>
        /// Locks the recoverability action for further changes.
        /// </summary>
        IRecoverabilityActionContext PreventChanges();
    }
}