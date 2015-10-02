namespace NServiceBus.Core.Tests
{
    using NServiceBus.Extensibility;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline.Contexts;

    class ContextHelpers
    {
        public static OutgoingLogicalMessageContext GetOutgoingContext(ExtendableOptions options)
        {
            var context = GetOutgoingContext(new MyMessage());

            context.Merge(options.Context);

            return context;
        }

        class MyMessage { }

        public static OutgoingLogicalMessageContext GetOutgoingContext(object message)
        {
            return new OutgoingLogicalMessageContext(new OutgoingLogicalMessage(message), null, null);
        }
    }
}