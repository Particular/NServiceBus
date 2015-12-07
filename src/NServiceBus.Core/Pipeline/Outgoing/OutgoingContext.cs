namespace NServiceBus.OutgoingPipeline
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;

    /// <summary>
    /// The base interface for everything inside the outgoing pipeline.
    /// </summary>
    public interface OutgoingContext : BehaviorContext, IBusContext
    {
        /// <summary>
        /// The id of the outgoing message.
        /// </summary>
        string MessageId { get; }

        /// <summary>
        /// The headers of the outgoing message.
        /// </summary>
        Dictionary<string, string> Headers { get; }
    }
}