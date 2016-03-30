namespace NServiceBus.Testing
{
    using System;
    using NServiceBus.Pipeline;

    /// <summary>
    /// A testable implementation of <see cref="IUnsubscribeContext" />.
    /// </summary>
    public class TestableUnsubscribeContext : TestableBehaviorContext, IUnsubscribeContext
    {
        /// <summary>
        /// The type of the event.
        /// </summary>
        public Type EventType { get; set; } = typeof(object);
    }
}