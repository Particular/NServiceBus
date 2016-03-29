namespace NServiceBus.Testing
{
    using NServiceBus.Pipeline;

    /// <summary>
    /// A testable implementation of <see cref="IOutgoingReplyContext" />.
    /// </summary>
    public class TestableOutgoingReplyContext : TestableOutgoingContext, IOutgoingReplyContext
    {
        /// <summary>
        /// The reply message.
        /// </summary>
        public OutgoingLogicalMessage Message { get; set; } = new OutgoingLogicalMessage(typeof(object), new object());
    }
}