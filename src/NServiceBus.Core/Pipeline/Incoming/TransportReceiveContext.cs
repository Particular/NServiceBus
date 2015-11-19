namespace NServiceBus.Pipeline.Contexts
{
    using NServiceBus.Unicast.Transport;
    using Transports;

    /// <summary>
    /// Context containing a physical message.
    /// </summary>
    public interface TransportReceiveContext : BehaviorContext
    {
        /// <summary>
        /// The physical message beeing processed.
        /// </summary>
        IncomingMessage Message { get; }

        /// <summary>
        /// Information about the current pipeline.
        /// </summary>
        PipelineInfo PipelineInfo { get; }
    }

    /// <summary>
    /// Context containing a physical message.
    /// </summary>
    class TransportReceiveContextImpl : BehaviorContextImpl, TransportReceiveContext
    {
        /// <summary>
        /// Initializes the transport receive stage context.
        /// </summary>
        public TransportReceiveContextImpl(IncomingMessage receivedMessage, PipelineInfo pipelineInfo, BehaviorContext parentContext)
            : base(parentContext)
        {
            Message = receivedMessage;
            PipelineInfo = pipelineInfo;

            Set(Message);
        }

        /// <summary>
        /// The physical message beeing processed.
        /// </summary>
        public IncomingMessage Message { get; }

        /// <summary>
        /// Information about the current pipeline.
        /// </summary>
        public PipelineInfo PipelineInfo { get;}
    }
}