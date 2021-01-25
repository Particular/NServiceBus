namespace NServiceBus.Testing
{
    using System;
    using System.ComponentModel;
    using Pipeline;

    /// <summary>
    /// A testable implementation of <see cref="ISubscribeContext" />.
    /// </summary>
    public partial class TestableSubscribeContext : TestableBehaviorContext, ISubscribeContext
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Type EventType => throw new NotImplementedException();

        /// <summary>
        /// The types of the events.
        /// </summary>
        public Type[] EventTypes { get; set; } = { typeof(object) };
    }
}