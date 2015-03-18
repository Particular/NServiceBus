namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Transactions;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;

    class MsmqReceiveWithTransactionScopeBehavior : MsmqReceiveBehavior
    {
        public MsmqReceiveWithTransactionScopeBehavior(TransactionOptions transactionOptions)
        {
            this.transactionOptions = transactionOptions;
        }

        protected override void Invoke(IncomingContext context, Action<IncomingMessage> onMessage)
        {
            var queue = context.Get<MessageQueue>();

            using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions))
            {
                Message message;

                if (!TryReceiveMessage(() => queue.Receive(TimeSpan.FromSeconds(1), MessageQueueTransactionType.Automatic), context, out message))
                {
                    scope.Complete();
                    return;
                }

                Dictionary<string,string> headers;

                try
                {
                    headers = MsmqUtilities.ExtractHeaders(message);
                }
                catch (Exception ex)
                {
                    HandleCorruptMessage(context,message,ex,(q,m)=> q.Send(m, MessageQueueTransactionType.Automatic));

                    scope.Complete();
                    return;
                }


                using (var bodyStream = message.BodyStream)
                {
                    onMessage(new IncomingMessage(message.Id, headers, bodyStream));
                }

                scope.Complete();
            }
        }

        readonly TransactionOptions transactionOptions;
    }
}