namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using NServiceBus.Extensibility;
    using NServiceBus.Logging;
    using NServiceBus.Transports;

    class ReceiveWithNoTransaction : ReceiveStrategy
    {
        public override void ReceiveMessage(MessageQueue inputQueue, MessageQueue errorQueue, Action<PushContext> onMessage)
        {

            var message = inputQueue.Receive(TimeSpan.FromSeconds(1), MessageQueueTransactionType.None);

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
            
                onMessage(new PushContext(incomingMessage,  new ContextBag()));
            }
        }

        static ILog Logger = LogManager.GetLogger<ReceiveWithNoTransaction>();
    }
}