namespace NServiceBus.Testing
{
    using NServiceBus.Pipeline;

    /// <summary>
    /// A testable implementation of <see cref="IOutgoingSendContext" />.
    /// </summary>
    public class TestableOutgoingSendContext : TestableOutgoingContext, IOutgoingSendContext
    {
        /// <summary>
        /// The message being sent.
        /// </summary>
        public OutgoingLogicalMessage Message { get; set; } = new OutgoingLogicalMessage(typeof(object), new object());
    }
}