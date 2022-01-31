namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Extensibility;
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
        public ErrorContext ErrorContext { get; set; } = new ErrorContext(
            new Exception(),
            new Dictionary<string, string>(),
            Guid.NewGuid().ToString(),
            ReadOnlyMemory<byte>.Empty,
            new TransportTransaction(),
            0,
            "",
            new ContextBag());

        /// <summary>
        /// The recoverability action to take for this message.
        /// </summary>
        public RecoverabilityAction RecoverabilityAction { get; set; }

        /// <summary>
        /// Locks the recoverability action for further changes.
        /// </summary>
        public void PreventChanges()
        {
            IsLocked = true;
        }

        /// <summary>
        /// True if the recoverability action was locked.
        /// </summary>
        public bool IsLocked { get; set; } = false;
    }
}