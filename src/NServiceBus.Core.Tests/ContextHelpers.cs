namespace NServiceBus.Core.Tests
{
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline.Contexts;

    class ContextHelpers
    {
        public static OutgoingContext GetOutgoingContext(ExtendableOptions options)
        {
            return new OutgoingContext(null,typeof(MyMessage),new MyMessage(), options);
        }

        class MyMessage { }

        public static OutgoingContext GetOutgoingContext(object message)
        {
            return new OutgoingContext(null, message.GetType(), message, new SendOptions());
        }
    }
}