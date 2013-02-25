namespace NServiceBus.RabbitMq
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Transactions;
    using global::RabbitMQ.Client;

    public class RabbitMqUnitOfWork
    {
        public IManageRabbitMqConnections ConnectionManager{ get; set; }

        public void Add(Action<IModel> action)
        {
            var transaction = Transaction.Current;

            if (transaction == null)
            {
                using (var channel = ConnectionManager.GetConnection(ConnectionPurpose.Publish).CreateModel())
                    action(channel);
               
                return;
            }

            var transactionId = transaction.TransactionInformation.LocalIdentifier;

            if (!OutstandingOperations.ContainsKey(transactionId))
            {
                transaction.TransactionCompleted += ExecuteActionsAgainstRabbitMq;
                OutstandingOperations.Add(transactionId, new List<Action<IModel>> { action });
                return;
            }

            OutstandingOperations[transactionId].Add(action);
        }

        void ExecuteActionsAgainstRabbitMq(object sender, TransactionEventArgs transactionEventArgs)
        {
            var transactionInfo = transactionEventArgs.Transaction.TransactionInformation; 

            if (transactionInfo.Status != TransactionStatus.Committed)
            {
                OutstandingOperations.Clear();
                return;
            }

            var transactionId = transactionInfo.LocalIdentifier;

            if (!OutstandingOperations.ContainsKey(transactionId))
                return;

            var actions = OutstandingOperations[transactionId];
            
            if (!actions.Any())
                return;

            using (var channel = ConnectionManager.GetConnection(ConnectionPurpose.Publish).CreateModel())
            {
                foreach (var action in actions)
                {
                    action(channel);
                }
            }

            OutstandingOperations.Clear();
        }

      

        IDictionary<string, IList<Action<IModel>>> OutstandingOperations
        {
            get {
                return outstandingOperations ??(outstandingOperations = new Dictionary<string, IList<Action<IModel>>>());
            }
        }


        //we use a dictionary to make sure that actions from other tx doesn't spill over if threads are getting reused by the hosting infrastrcture
        [ThreadStatic] 
        static IDictionary<string, IList<Action<IModel>>> outstandingOperations;
    }
}