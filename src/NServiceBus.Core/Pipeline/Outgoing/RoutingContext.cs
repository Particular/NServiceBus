namespace NServiceBus.TransportDispatch
{
    using System.Collections.Generic;
    using Routing;
    using Pipeline;
    using Transports;

    /// <summary>
    /// Context for the dispatch part of the pipeline.
    /// </summary>
    public class RoutingContext : BehaviorContext
    {
        /// <summary>
        /// Initializes the context with the message to be dispatched.
        /// </summary>
        public RoutingContext(OutgoingMessage messageToDispatch, IReadOnlyCollection<AddressLabel> addressLabels, BehaviorContext context) : base(context)
        {
            Message = messageToDispatch;
            AddressLabels = addressLabels;
        }

        /// <summary>
        /// Initializes the context with the message to be dispatched.
        /// </summary>
        public RoutingContext(OutgoingMessage messageToDispatch, AddressLabel addressLabel, BehaviorContext context) : base(context)
        {
            Message = messageToDispatch;
            AddressLabels = new [] {addressLabel};
        }

        /// <summary>
        /// The message to dispatch the the transport.
        /// </summary>
        public OutgoingMessage Message { get; private set; }

        /// <summary>
        /// The routing strategy for the operation to be dispatched.
        /// </summary>
        public IReadOnlyCollection<AddressLabel> AddressLabels { get; set; }
    }
}