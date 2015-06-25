namespace NServiceBus.Core.Tests
{
    using NServiceBus.Extensibility;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline.Contexts;

    class ContextHelpers
    {
        public static OutgoingContext GetOutgoingContext(ExtendableOptions options)
        {
            var context = GetOutgoingContext(new MyMessage());

            context.Merge(options.Context);

            return context;
        }

        class MyMessage { }

        public static OutgoingContext GetOutgoingContext(object message)
        {
            var context = new OutgoingContext(null);

            context.Set(new OutgoingLogicalMessage(message));

            return context;
        }
    }
}