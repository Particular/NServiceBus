namespace NServiceBus.Testing
{
    using System;
    using NServiceBus.Pipeline;
    /// <summary>
    /// A testable implementation of <see cref="ISubscribeContext" />.
    /// </summary>
    public class TestableSubscribeContext : TestableBehaviorContext, ISubscribeContext
    {
        /// <summary>
        /// The type of the event.
        /// </summary>
        public Type EventType { get; set; } = typeof(object);
    }
}