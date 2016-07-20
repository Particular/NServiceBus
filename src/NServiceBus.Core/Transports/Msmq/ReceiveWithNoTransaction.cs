namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading.Tasks;
    using Transport;

    class ReceiveWithNoTransaction : ReceiveStrategy
    {
        public override async Task ReceiveMessage()
        {
            Message message;

            if (!TryReceive(MessageQueueTransactionType.None, out message))
            {
                return;
            }

            Dictionary<string, string> headers;

            var transportTransaction = new TransportTransaction();

            Exception ex;
            if ((ex = TryExtractHeaders(message, out headers)) != null)
            {
                await HandleError(message, new Dictionary<string, string>(), ex, transportTransaction, 1, isPoison: true).ConfigureAwait(false);
                return;
            }

            using (var bodyStream = message.BodyStream)
            {
                try
                {
                    await TryProcessMessage(message, headers, bodyStream, transportTransaction).ConfigureAwait(false);
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