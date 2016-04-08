namespace NServiceBus.Core.Tests
{
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline;
    using Testing;

    class ContextHelpers
    {
        public static IOutgoingLogicalMessageContext GetOutgoingContext(ExtendableOptions options)
        {
            var context = GetOutgoingContext(new MyMessage());

            context.Extensions.Merge(options.Context);

            return context;
        }

        class MyMessage { }

        public static IOutgoingLogicalMessageContext GetOutgoingContext(object message)
        {
            return new TestableOutgoingLogicalMessageContext {Message = new OutgoingLogicalMessage(message.GetType(), message) };
        }
    }
}