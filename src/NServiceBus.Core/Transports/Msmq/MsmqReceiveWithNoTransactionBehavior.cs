namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;

    class MsmqReceiveWithNoTransactionBehavior : MsmqReceiveBehavior
    {
        protected override void Invoke(IncomingContext context, Action<IncomingMessage> onMessage)
        {
            var queue = context.Get<MessageQueue>();

            Message message;

            if (!TryReceiveMessage(() => queue.Receive(TimeSpan.FromSeconds(1), MessageQueueTransactionType.None), context, out message))
            {
                return;
            }

            Dictionary<string,string> headers;

            try
            {
                headers = MsmqUtilities.ExtractHeaders(message);
            }
            catch (Exception ex)
            {
                HandleCorruptMessage(context, message, ex, (q, m) => q.Send(m, MessageQueueTransactionType.None));
                return;
            }

            using (var bodyStream = message.BodyStream)
            {
                onMessage(new IncomingMessage(message.Id,headers,bodyStream));
            }
        }
    }
}