namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NUnit.Framework;
    using Transport;

    public class When_transaction_level_TransactionScope : NServiceBusTransportTest
    {
        //[TestCase(TransportTransactionMode.None)] -- not relevant
        //[TestCase(TransportTransactionMode.ReceiveOnly)] -- not relevant
        //[TestCase(TransportTransactionMode.SendsAtomicWithReceive)] -- not relevant
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_have_active_transaction(TransportTransactionMode transactionMode)
        {
            var messageHandled = new TaskCompletionSource<Tuple<Transaction, Transaction>>();

            await StartPump(
                (context, _) =>
                {
                    var currentTransaction = Transaction.Current;
                    var contextTransaction = context.TransportTransaction.Get<Transaction>();
                    messageHandled.SetResult(new Tuple<Transaction, Transaction>(currentTransaction, contextTransaction));

                    return Task.FromResult(0);
                },
                errorContext => Task.FromResult(ErrorHandleResult.Handled),
                transactionMode);
            await SendMessage(InputQueueName);

            var transactions = await messageHandled.Task;

            Assert.That(transactions.Item1, Is.Not.Null);
            Assert.That(transactions.Item2, Is.Not.Null);
            Assert.AreSame(transactions.Item1, transactions.Item2);
        }
    }
}