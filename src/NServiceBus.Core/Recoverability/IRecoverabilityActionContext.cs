namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using Transport;

    /// <summary>
    /// Provide context to behaviors on the recoverability action pipeline.
    /// </summary>
    public interface IRecoverabilityActionContext : IBehaviorContext
    {
        /// <summary>
        /// Context for the message that failed processing.
        /// </summary>
        ErrorContext ErrorContext { get; }

        /// <summary>
        /// Metadata for this message.
        /// </summary>
        IReadOnlyDictionary<string, string> Metadata { get; }
    }
}