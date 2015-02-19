namespace NServiceBus
{
    using System;
    using System.Messaging;
    using NServiceBus.Pipeline.Contexts;

    class MsmqReceiveWithNoTransactionBehavior : MsmqReceiveBehavior
    {
        protected override void Invoke(IncomingContext context, Action<TransportMessage> onMessage)
        {
            var queue = context.Get<MessageQueue>();

            Message message;

            if (!TryReceiveMessage(() => queue.Receive(TimeSpan.FromSeconds(1), MessageQueueTransactionType.None), context, out message))
            {
                return;
            }

            TransportMessage transportMessage;

            try
            {
                transportMessage = MsmqUtilities.Convert(message);
            }
            catch (Exception ex)
            {
                HandleCorruptMessage(context, message, ex, (q, m) => q.Send(m, MessageQueueTransactionType.None));
                return;
            }


            onMessage(transportMessage);
        }
    }
}