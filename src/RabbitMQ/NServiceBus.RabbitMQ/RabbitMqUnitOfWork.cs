namespace NServiceBus.Transports.RabbitMQ
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Transactions;
    using global::RabbitMQ.Client;
    using global::RabbitMQ.Client.Exceptions;
    using Unicast.Queuing;

    public class RabbitMqUnitOfWork
    {
        public IManageRabbitMqConnections ConnectionManager { get; set; }

        /// <summary>
        /// If set to true pulisher confirms will be used to make sure that messages are acked by the broker before considered to be published
        /// </summary>
        public bool UsePublisherConfirms { get; set; }

        /// <summary>
        /// The maximum time to wait for all publisher confirms to be received
        /// </summary>
        public TimeSpan MaxWaitTimeForConfirms { get; set; }

        public void Add(Action<IModel> action)
        {
            var transaction = Transaction.Current;

            if (transaction == null)
            {
                ExecuteRabbitMqActions(new[] { action });

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

            ExecuteRabbitMqActions(actions);

            OutstandingOperations.Clear();
        }

        void ExecuteRabbitMqActions(IList<Action<IModel>> actions)
        {
            using (var channel = ConnectionManager.GetConnection(ConnectionPurpose.Publish).CreateModel())
            {
                if (UsePublisherConfirms)
                {
                    channel.ConfirmSelect();
                }


                foreach (var action in actions)
                {
                    action(channel);
                }
                try
                {
                    channel.WaitForConfirmsOrDie(MaxWaitTimeForConfirms);
                }
                catch (AlreadyClosedException ex)
                {
                    if (ex.ShutdownReason != null && ex.ShutdownReason.ReplyCode == 404)
                    {
                        var msg = ex.ShutdownReason.ReplyText;
                        var matches = Regex.Matches(msg, @"'([^' ]*)'");
                        var exchangeName = matches.Count > 0 && matches[0].Groups.Count > 1 ? Address.Parse(matches[0].Groups[1].Value) : null;
                        throw new QueueNotFoundException(exchangeName, "Exchange for the recipient does not exist", ex);
                    }
                    throw;
                }
            }
        }


        IDictionary<string, IList<Action<IModel>>> OutstandingOperations
        {
            get
            {
                return outstandingOperations ?? (outstandingOperations = new Dictionary<string, IList<Action<IModel>>>());
            }
        }


        //we use a dictionary to make sure that actions from other tx doesn't spill over if threads are getting reused by the hosting infrastrcture
        [ThreadStatic]
        static IDictionary<string, IList<Action<IModel>>> outstandingOperations;
    }
}