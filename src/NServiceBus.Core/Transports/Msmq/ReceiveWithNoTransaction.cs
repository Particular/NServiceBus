namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Logging;
    using NServiceBus.Transports;

    class ReceiveWithNoTransaction : ReceiveStrategy
    {
        public override async Task ReceiveMessage(MessageQueue inputQueue, MessageQueue errorQueue, Func<PushContext, Task> onMessage)
        {

            var message = inputQueue.Receive(TimeSpan.FromMilliseconds(10), MessageQueueTransactionType.None);

            Dictionary<string, string> headers;

            try
            {
                headers = MsmqUtilities.ExtractHeaders(message);
            }
            catch (Exception ex)
            {
                var error = string.Format("Message '{0}' is corrupt and will be moved to '{1}'", message.Id, errorQueue.QueueName);
                Logger.Error(error, ex);

                errorQueue.Send(message, MessageQueueTransactionType.None);

                return;
            }

            using (var bodyStream = message.BodyStream)
            {
                var incomingMessage = new IncomingMessage(message.Id, headers, bodyStream);

                await onMessage(new PushContext(incomingMessage, new ContextBag())).ConfigureAwait(false);
            }
        }

        static ILog Logger = LogManager.GetLogger<ReceiveWithNoTransaction>();
    }
}