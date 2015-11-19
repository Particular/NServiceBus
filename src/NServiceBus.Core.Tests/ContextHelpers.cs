namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Extensibility;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Pipeline.OutgoingPipeline;

    class ContextHelpers
    {
        public static OutgoingLogicalMessageContext GetOutgoingContext(ExtendableOptions options)
        {
            var context = GetOutgoingContext(new MyMessage());

            context.Extensions.Merge(options.Context);

            return context;
        }

        class MyMessage { }

        public static OutgoingLogicalMessageContext GetOutgoingContext(object message)
        {
            return new OutgoingLogicalMessageContextImpl(
                Guid.NewGuid().ToString(),
                new Dictionary<string, string>(),
                new OutgoingLogicalMessage(message),
                null,
                new RootContext(null));
        }
    }
}