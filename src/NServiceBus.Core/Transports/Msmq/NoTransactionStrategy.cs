namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading.Tasks;
    using Transport;

    class NoTransactionStrategy : ReceiveStrategy
    {
        public override async Task ReceiveMessage()
        {
            Message message;

            if (!TryReceive(MessageQueueTransactionType.None, out message))
            {
                return;
            }

            Dictionary<string, string> headers;

            if (!TryExtractHeaders(message, out headers))
            {
                MovePoisonMessageToErrorQueue(message, IsQueuesTransactional ? MessageQueueTransactionType.Single : MessageQueueTransactionType.None);
                return;
            }

            var transportTransaction = new TransportTransaction();

            using (var bodyStream = message.BodyStream)
            {
                try
                {
                    await TryProcessMessage(message.Id, headers, bodyStream, transportTransaction).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    message.BodyStream.Position = 0;

                    await HandleError(message, headers, exception, transportTransaction, 1).ConfigureAwait(false);
                }
            }
        }
    }
}