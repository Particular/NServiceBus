namespace NServiceBus.Testing
{
    using System;
    using Pipeline;

    /// <summary>
    /// A testable implementation of <see cref="IUnsubscribeContext" />.
    /// </summary>
    public partial class TestableUnsubscribeContext : TestableBehaviorContext, IUnsubscribeContext
    {
        /// <summary>
        /// The type of the event.
        /// </summary>
        public Type EventType { get; set; } = typeof(object);
    }
}