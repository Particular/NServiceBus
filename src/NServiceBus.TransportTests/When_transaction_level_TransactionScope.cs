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

            var completed = new TaskCompletionSource<bool>();
            OnTestTimeout(() => completed.SetCanceled());

            await StartPump(
                (context, _) =>
                {
                    currentTransaction = Transaction.Current;
                    contextTransaction = context.TransportTransaction.Get<Transaction>();
                    return Task.CompletedTask;
                },
                (_, __) => Task.FromResult(ReceiveResult.Discarded),
                (_, __) => completed.SetCompleted(),
                transactionMode);

            await SendMessage(InputQueueName);

            _ = await completed.Task;

            Assert.NotNull(currentTransaction);
            Assert.AreSame(currentTransaction, contextTransaction);
        }
    }
}
