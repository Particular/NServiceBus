namespace NServiceBus.Testing
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// A testable implementation of <see cref="IDispatchContext"/>.
    /// </summary>
    public class TestableDispatchContext : TestableBehaviorContext, IDispatchContext
    {
        IEnumerable<TransportOperation> IDispatchContext.Operations => Operations;

        /// <summary>
        /// The operations to be dispatched to the transport.
        /// </summary>
        public IList<TransportOperation> Operations { get; set; } = new List<TransportOperation>();
    }
}