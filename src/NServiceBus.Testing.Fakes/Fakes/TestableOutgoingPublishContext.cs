namespace NServiceBus.Testing
{
    using NServiceBus.Pipeline;

    /// <summary>
    /// A testable implementation of <see cref="IOutgoingPublishContext" />.
    /// </summary>
    public class TestableOutgoingPublishContext : TestableOutgoingContext, IOutgoingPublishContext
    {
        /// <summary>
        /// The message to be published.
        /// </summary>
        public OutgoingLogicalMessage Message { get; set; } = new OutgoingLogicalMessage(typeof(object), new object());
    }
}