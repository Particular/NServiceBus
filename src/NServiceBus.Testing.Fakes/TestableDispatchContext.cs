﻿namespace NServiceBus.Testing
{
    using System.Collections.Generic;
    using Pipeline;
    using Transport;

    /// <summary>
    /// A testable implementation of <see cref="IDispatchContext" />.
    /// </summary>
    public partial class TestableDispatchContext : TestableBehaviorContext, IDispatchContext
    {
        /// <summary>
        /// The operations to be dispatched to the transport.
        /// </summary>
        public IList<TransportOperation> Operations { get; set; } = new List<TransportOperation>();

        IEnumerable<TransportOperation> IDispatchContext.Operations => Operations;
    }
}