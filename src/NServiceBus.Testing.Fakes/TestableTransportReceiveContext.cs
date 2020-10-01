namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using Pipeline;
    using Transport;

    /// <summary>
    /// A testable implementation for <see cref="ITransportReceiveContext" />.
    /// </summary>
    public partial class TestableTransportReceiveContext : TestableBehaviorContext, ITransportReceiveContext
    {
        /// <summary>
        /// Indicated whether <see cref="AbortReceiveOperation" /> has been called or not.
        /// </summary>
        public bool ReceiveOperationAborted { get; set; }

        /// <summary>
        /// Allows the pipeline to flag that it has been aborted and the receive operation should be rolled back.
        /// </summary>
        public virtual void AbortReceiveOperation()
        {
            ReceiveOperationAborted = true;
        }

        /// <summary>
        /// The physical message being processed.
        /// </summary>
        public IncomingMessage Message { get; set; } = new IncomingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]);
    }
}