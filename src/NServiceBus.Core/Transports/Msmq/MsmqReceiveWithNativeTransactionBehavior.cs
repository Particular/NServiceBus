namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;

    class MsmqReceiveWithNativeTransactionBehavior : MsmqReceiveBehavior
    {
        protected override void Invoke(IncomingContext context, Action<IncomingMessage> onMessage)
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

                    Dictionary<string,string> headers;

                    try
                    {
                        headers = MsmqUtilities.ExtractHeaders(message);
                    }
                    catch (Exception ex)
                    {
                        HandleCorruptMessage(context, message, ex, (q, m) => q.Send(m, msmqTransaction));

                        msmqTransaction.Commit();
                        return;
                    }

                    context.Set(msmqTransaction);
                    
                    using (var bodyStream = message.BodyStream)
                    {
                        onMessage(new IncomingMessage(message.Id, headers, bodyStream));
                    }

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