namespace NServiceBus.Testing
{
    using Pipeline;

    /// <summary>
    /// A testable implementation of <see cref="IOutgoingPublishContext" />.
    /// </summary>
    public partial class TestableOutgoingPublishContext : TestableOutgoingContext, IOutgoingPublishContext
    {
        /// <summary>
        /// The message to be published.
        /// </summary>
        public OutgoingLogicalMessage Message { get; set; } = new OutgoingLogicalMessage(typeof(object), new object());
    }
}