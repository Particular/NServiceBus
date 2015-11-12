namespace NServiceBus.Pipeline.Contexts
{
    using NServiceBus.Unicast.Transport;
    using Transports;

    /// <summary>
    /// Context containing a physical message.
    /// </summary>
    public class TransportReceiveContext : BehaviorContext
    {
        /// <summary>
        /// Initializes the transport receive stage context.
        /// </summary>
        public TransportReceiveContext(IncomingMessage receivedMessage, PipelineInfo pipelineInfo, BehaviorContext parentContext)
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