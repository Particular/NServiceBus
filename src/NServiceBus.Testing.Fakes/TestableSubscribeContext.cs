namespace NServiceBus.Testing
{
    using System;
    using Pipeline;

    /// <summary>
    /// A testable implementation of <see cref="ISubscribeContext" />.
    /// </summary>
    public partial class TestableSubscribeContext : TestableBehaviorContext, ISubscribeContext
    {
        /// <summary>
        /// The types of the events.
        /// </summary>
        public Type[] EventTypes { get; set; } = new Type[0];
    }
}