﻿namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    /// <summary>
    /// A context of behavior execution in physical message processing stage.
    /// </summary>
    public interface PhysicalMessageProcessingContext : IncomingContext
    {
        /// <summary>
        /// The physical message beeing processed.
        /// </summary>
        IncomingMessage Message { get; }
    }

    /// <summary>
    /// A context of behavior execution in physical message processing stage.
    /// </summary>
    class PhysicalMessageProcessingContextImpl : IncomingContextBase, PhysicalMessageProcessingContext
    {
        internal PhysicalMessageProcessingContextImpl(TransportReceiveContext parentContext)
            : this(parentContext.Message, parentContext.PipelineInfo, parentContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PhysicalMessageProcessingContext" />.
        /// </summary>
        public PhysicalMessageProcessingContextImpl(IncomingMessage message, PipelineInfo pipelineInfo, BehaviorContext parentContext)
            : base(message.MessageId, message.GetReplyToAddress(), message.Headers, pipelineInfo, parentContext)
        {
            Message = message;
        }

        /// <summary>
        /// The physical message beeing processed.
        /// </summary>
        public IncomingMessage Message { get; private set; }
    }
}