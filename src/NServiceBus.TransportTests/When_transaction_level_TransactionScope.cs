namespace NServiceBus.TransportTests
{
    using System.Threading.Tasks;
    using System.Transactions;
    using NUnit.Framework;
    using Transport;

    public class When_transaction_level_TransactionScope : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_have_active_transaction(TransportTransactionMode transactionMode)
        {
            Transaction currentTransaction = null;
            Transaction contextTransaction = null;

            var messageProcessed = new TaskCompletionSource();
            OnTestTimeout(() => messageProcessed.SetCanceled());

            await StartPump(
                (context, _) =>
                {
                    currentTransaction = Transaction.Current;
                    contextTransaction = context.TransportTransaction.Get<Transaction>();
                    return messageProcessed.SetCompleted();
                },
                (_, __) => Task.FromResult(ErrorHandleResult.Handled),
                transactionMode);

            await SendMessage(InputQueueName);

            await messageProcessed.Task;

            Assert.NotNull(currentTransaction);
            Assert.AreSame(currentTransaction, contextTransaction);
        }
    }
}
