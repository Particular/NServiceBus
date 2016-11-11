﻿// ReSharper disable PartialTypeWithSinglePart
namespace NServiceBus.Testing
{
    using ObjectBuilder;
    using Pipeline;

    /// <summary>
    /// Base implementation for contexts implementing <see cref="IIncomingContext" />.
    /// </summary>
    public abstract partial class TestableIncomingContext : TestableMessageProcessingContext, IIncomingContext
    {
        /// <summary>
        /// A fake <see cref="IBuilder" /> implementation. If you want to provide your own <see cref="IBuilder" /> implementation
        /// override <see cref="GetBuilder" />.
        /// </summary>
        public FakeBuilder Builder { get; set; } = new FakeBuilder();

        IBuilder IBehaviorContext.Builder => GetBuilder();

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