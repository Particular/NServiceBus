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


    class MsmqReceiveWithNativeTransactionBehavior : MsmqReceiveBehavior
    {
        protected override void Invoke(IncomingContext context, Action<TransportMessage> onMessage)
        {
            var queue = context.Get<MessageQueue>();

            using (var msmqTransaction = new MessageQueueTransaction())
            {
                try
                {
                    msmqTransaction.Begin();

                    Message message;

                    if (!TryReceiveMessage(() => queue.Receive(TimeSpan.FromSeconds(1), msmqTransaction), context, out message))
                    {
                        msmqTransaction.Commit();
                        return;
                    }

                    TransportMessage transportMessage;

                    try
                    {
                        transportMessage = MsmqUtilities.Convert(message);
                    }
                    catch (Exception ex)
                    {
                        HandleCorruptMessage(context, message, ex, (q, m) => q.Send(m, msmqTransaction));

                        msmqTransaction.Commit();
                        return;
                    }

                    //todo: add tx to UoW
                    onMessage(transportMessage);
                    //finally
                    //{
                    //    //todo: clear tx to UoW
                    //}
                    msmqTransaction.Commit();
                }
                catch (Exception)
                {
                    msmqTransaction.Abort();
                }
           }




        }
    }
}