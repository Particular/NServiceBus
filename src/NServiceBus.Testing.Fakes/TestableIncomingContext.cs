// ReSharper disable PartialTypeWithSinglePart
namespace NServiceBus.Testing
{
    using System;
    using ObjectBuilder;
    using Pipeline;

    /// <summary>
    /// Base implementation for contexts implementing <see cref="IIncomingContext" />.
    /// </summary>
    public abstract partial class TestableIncomingContext : TestableMessageProcessingContext, IIncomingContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="TestableIncomingContext" />.
        /// </summary>
        protected TestableIncomingContext(IMessageCreator messageCreator = null) : base(messageCreator)
        {
        }

        /// <summary>
        /// A fake <see cref="IServiceProvider" /> implementation. If you want to provide your own <see cref="IBuilder" /> implementation
        /// override <see cref="GetBuilder" />.
        /// </summary>
        public FakeBuilder Builder { get; set; } = new FakeBuilder();

        IServiceProvider IBehaviorContext.Builder => GetBuilder();

        /// <summary>
        /// Selects the builder returned by <see cref="IBehaviorContext.Builder" />. Override this method to provide your custom
        /// <see cref="IServiceProvider" /> implementation.
        /// </summary>
        protected virtual IServiceProvider GetBuilder()
        {
            return Builder;
        }
    }
}