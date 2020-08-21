// ReSharper disable PartialTypeWithSinglePart
namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;
    using ObjectBuilder;
    using Pipeline;

    /// <summary>
    /// A base implementation for all behaviors implementing <see cref="IOutgoingContext" />.
    /// </summary>
    public partial class TestableOutgoingContext : TestablePipelineContext, IOutgoingContext
    {
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

        /// <summary>
        /// The id of the outgoing message.
        /// </summary>
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The headers of the outgoing message.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }
}