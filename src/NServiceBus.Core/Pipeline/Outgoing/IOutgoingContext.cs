namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;

    /// <summary>
    /// The base interface for everything inside the outgoing pipeline.
    /// </summary>
    public interface IOutgoingContext : IBehaviorContext, IPipelineContext
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