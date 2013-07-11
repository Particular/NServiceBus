using MyMessages.DataBus;

namespace Subscriber1
{
    using System;
    using NServiceBus;
    using NServiceBus.Logging;

    public class MessageWithLargePayloadHandler : IHandleMessages<MessageWithLargePayload>
    {
        public void Handle(MessageWithLargePayload message)
        {
            Logger.Info(string.Format("Subscriber 1 received MessageWithLargePayload with SomeProperty {0}.", message.SomeProperty));

            Console.WriteLine("Message received, size of blob property: " + message.LargeBlob.Value.Length + " Bytes");
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(MessageWithLargePayloadHandler));

    }
}