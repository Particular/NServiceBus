namespace NServiceBus
{
    using System;
    using System.Messaging;
    using System.Transactions;
    using NServiceBus.Pipeline.Contexts;

    class MsmqReceiveWithTransactionScopeBehavior : MsmqReceiveBehavior
    {
        public MsmqReceiveWithTransactionScopeBehavior(TransactionOptions transactionOptions)
        {
            this.transactionOptions = transactionOptions;
        }

        protected override void Invoke(IncomingContext context, Action<TransportMessage> onMessage)
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

                TransportMessage transportMessage;

                try
                {
                    transportMessage = MsmqUtilities.Convert(message);
                }
                catch (Exception ex)
                {
                    HandleCorruptMessage(context,message,ex,(q,m)=> q.Send(m, MessageQueueTransactionType.Automatic));

                    scope.Complete();
                    return;
                }


                onMessage(transportMessage);

                scope.Complete();
            }
        }

        readonly TransactionOptions transactionOptions;
    }
}