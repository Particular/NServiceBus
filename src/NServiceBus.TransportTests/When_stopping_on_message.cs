namespace NServiceBus.TransportTests
{
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_stopping_on_message : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_complete(TransportTransactionMode transactionMode)
        {
            CancellationToken onMessageToken = default;

            var onMessageStarted = CreateTaskCompletionSource();

            var pumpStopping = CreateTaskCompletionSource();

            await StartPump(
                async (_, cancellationToken) =>
                {
                    onMessageStarted.SetResult();
                    await pumpStopping.Task;
                    onMessageToken = cancellationToken;
                },
                (_, __) => Task.FromResult(ErrorHandleResult.Handled),
                transactionMode);

            await SendMessage(InputQueueName);

            await onMessageStarted.Task;

            var pumpTask = StopPump();
            pumpStopping.SetResult();

            await pumpTask;

            Assert.False(onMessageToken.IsCancellationRequested);
        }
    }
}
