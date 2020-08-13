// ReSharper disable PartialTypeWithSinglePart
namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using ObjectBuilder;
    using Pipeline;

    /// <summary>
    /// A base implementation for all behaviors implementing <see cref="IOutgoingContext" />.
    /// </summary>
    public partial class TestableOutgoingContext : TestablePipelineContext, IOutgoingContext
    {
        /// <summary>
        /// A fake <see cref="IBuilder" /> implementation. If you want to provide your own <see cref="IBuilder" /> implementation
        /// override <see cref="GetBuilder" />.
        /// </summary>
        public FakeBuilder Builder { get; set; } = new FakeBuilder();

        IServiceProvider IBehaviorContext.Builder => GetBuilder();

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
        protected virtual IServiceProvider GetBuilder()
        {
            return Builder;
        }
    }
}