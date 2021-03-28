namespace NServiceBus.Testing
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using Extensibility;
    using Microsoft.Extensions.DependencyInjection;
    using ObjectBuilder;
    using Pipeline;

    /// <summary>
    /// A base implementation for contexts implementing <see cref="IBehaviorContext" />.
    /// </summary>
    public abstract partial class TestableBehaviorContext : IBehaviorContext
    {
        /// <summary>
        /// A <see cref="T:NServiceBus.Extensibility.ContextBag" /> which can be used to extend the current object.
        /// </summary>
        public ContextBag Extensions { get; set; } = new ContextBag();

        /// <summary>
        /// A fake <see cref="IServiceProvider" /> implementation. If you want to provide your own <see cref="IBuilder" /> implementation
        /// override <see cref="GetBuilder" />.
        /// </summary>
        public IServiceCollection Services { get; set; } = new ServiceCollection();

        IServiceProvider IBehaviorContext.Builder => GetBuilder();

        IServiceProvider builder = null;

        /// <summary>
        /// Selects the builder returned by <see cref="IBehaviorContext.Builder" />. Override this method to provide your custom
        /// <see cref="IServiceProvider" /> implementation.
        /// </summary>
        protected virtual IServiceProvider GetBuilder()
        {
            if (builder == null)
            {
                builder = Services.BuildServiceProvider();
            }
            return builder;
        }

        [SuppressMessage("Code", "PCR0003:Instance methods on types implementing ICancellableContext should not have a CancellationToken parameter", Justification = "Designed for testing.")]
        public CancellationToken CancellationToken { get; set; }
    }
}