namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading.Tasks;
    using Transports;

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

            if (!TryExtractHeaders(message, out headers))
            {
                MovePoisonMessageToErrorQueue(message, MessageQueueTransactionType.None);
                return;
            }

            try
            {
                await TryProcessMessage(message, headers, new TransportTransaction()).ConfigureAwait(false);
            }
            catch (Exception)
            {
                message.BodyStream.Position = 0;

                await OnError(new ErrorContext()).ConfigureAwait(false);
            }
        }
    }
}