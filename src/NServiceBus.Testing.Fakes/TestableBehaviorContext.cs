// ReSharper disable PartialTypeWithSinglePart
namespace NServiceBus.Testing
{
    using System;
    using Extensibility;
    using ObjectBuilder;
    using Pipeline;

    /// <summary>
    /// A base implementation for contexts implementing <see cref="IBehaviorContext" />.
    /// </summary>
    public abstract partial class TestableBehaviorContext : IBehaviorContext
    {
        /// <summary>
        /// A fake <see cref="IBuilder" /> implementation. If you want to provide your own <see cref="IBuilder" /> implementation
        /// override <see cref="GetBuilder" />.
        /// </summary>
        public FakeBuilder Builder { get; set; } = new FakeBuilder();

        /// <summary>
        /// A <see cref="T:NServiceBus.Extensibility.ContextBag" /> which can be used to extend the current object.
        /// </summary>
        public ContextBag Extensions { get; set; } = new ContextBag();

        IServiceProvider IBehaviorContext.Builder => GetBuilder();

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