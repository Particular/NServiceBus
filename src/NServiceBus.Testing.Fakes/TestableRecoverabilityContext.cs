namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using Pipeline;
    using Transport;

    /// <summary>
    /// A testable implementation of <see cref="IRecoverabilityContext" />.
    /// </summary>
    public partial class TestableRecoverabilityContext : TestableBehaviorContext, IRecoverabilityContext, IRecoverabilityActionContext
    {
        /// <summary>
        /// The message that failed processing.
        /// </summary>
        public IncomingMessage FailedMessage { get; set; } = new IncomingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), ReadOnlyMemory<byte>.Empty);

        /// <summary>
        /// The exception that caused processing to fail.
        /// </summary>
        public Exception Exception { get; set; } = new Exception();

        /// <summary>
        /// The receive address where this message failed.
        /// </summary>
        public string ReceiveAddress { get; set; } = "receive-queue";

        /// <summary>
        /// The number of times the message have been retried immediately but failed.
        /// </summary>
        public int ImmediateProcessingFailures { get; set; } = 0;

        /// <summary>
        /// Metadata for this message.
        /// </summary>
        IReadOnlyDictionary<string, string> IRecoverabilityActionContext.Metadata => Metadata;

        /// <summary>
        /// The recoverability configuration for the endpoint.
        /// </summary>
        public RecoverabilityConfig RecoverabilityConfiguration { get; set; } = new RecoverabilityConfig(
            new ImmediateConfig(0),
            new DelayedConfig(0, TimeSpan.Zero),
            new FailedConfig("error", new HashSet<Type>()));

        /// <summary>
        /// The recoverability action to take for this message.
        /// </summary>
        public RecoverabilityAction RecoverabilityAction { get; set; }

        /// <summary>
        /// Metadata for this message.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Locks the recoverability action for further changes.
        /// </summary>
        public IRecoverabilityActionContext PreventChanges()
        {
            IsLocked = true;
            return this;
        }

        /// <summary>
        /// True if the recoverability action was locked.
        /// </summary>
        public bool IsLocked { get; private set; } = false;
    }
}