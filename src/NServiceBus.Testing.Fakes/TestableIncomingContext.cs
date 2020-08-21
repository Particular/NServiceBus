// ReSharper disable PartialTypeWithSinglePart
namespace NServiceBus.Testing
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
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
        public IServiceCollection Services { get; set; } = new ServiceCollection();

        IServiceProvider IBehaviorContext.Builder => GetBuilder();

        IServiceProvider _builder = null;

        /// <summary>
        /// Selects the builder returned by <see cref="IBehaviorContext.Builder" />. Override this method to provide your custom
        /// <see cref="IServiceProvider" /> implementation.
        /// </summary>
        protected virtual IServiceProvider GetBuilder()
        {
            if (_builder == null)
            {
                _builder = Services.BuildServiceProvider();
            }
            return _builder;
        }
    }
}