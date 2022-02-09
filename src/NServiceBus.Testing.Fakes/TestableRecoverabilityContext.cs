namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using Extensibility;
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
        public ErrorContext ErrorContext { get; set; } = new ErrorContext(
            new Exception(),
            new Dictionary<string, string>(),
            Guid.NewGuid().ToString(),
            ReadOnlyMemory<byte>.Empty,
            new TransportTransaction(),
            0,
            "receive-address",
            new ContextBag());

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