namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using Pipeline;
    using Transport;

    /// <summary>
    /// A testable implementation of <see cref="IRecoverabilityContext" />.
    /// </summary>
    public partial class TestableRecoverabilityContext : TestableBehaviorContext, IRecoverabilityContext
    {
        /// <summary>
        /// The message that failed processing.
        /// </summary>
        public IncomingMessage FailedMessage { get; set; } = new IncomingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]);

        /// <summary>
        /// The recoverability action to take for the failed message.
        /// </summary>
        public ErrorHandleResult ActionToTake { get; set; }
    }
}