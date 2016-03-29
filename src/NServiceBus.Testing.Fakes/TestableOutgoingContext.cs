namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;

    /// <summary>
    /// A base implementation for all behaviors implementing <see cref="IOutgoingContext" />.
    /// </summary>
    public class TestableOutgoingContext : TestablePipelineContext, IOutgoingContext
    {
        /// <summary>
        /// A fake <see cref="IBuilder" /> implementation. If you want to provide your own <see cref="IBuilder" /> implementation
        /// override <see cref="GetBuilder" />.
        /// </summary>
        public FakeBuilder Builder { get; set; } = new FakeBuilder();

        IBuilder IBehaviorContext.Builder { get; }

        /// <summary>
        /// The id of the outgoing message.
        /// </summary>
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The headers of the outgoing message.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Selects the builder returned by <see cref="IBehaviorContext.Builder" />. Override this method to provide your custom
        /// <see cref="IBuilder" /> implementation.
        /// </summary>
        protected virtual IBuilder GetBuilder()
        {
            return Builder;
        }
    }
}