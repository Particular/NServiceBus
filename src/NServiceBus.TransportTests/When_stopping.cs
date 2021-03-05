namespace NServiceBus.TransportTests
{
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_stopping : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_allow_message_processing_to_complete(TransportTransactionMode transactionMode)
        {
            CancellationToken token = default;

            var messageProcessingStarted = new TaskCompletionSource();
            OnTestTimeout(() => messageProcessingStarted.SetCanceled());

            var pumpStopping = new TaskCompletionSource();

            await StartPump(
                async (_, cancellationToken) =>
                {
                    messageProcessingStarted.SetResult();
                    await pumpStopping.Task;
                    token = cancellationToken;
                },
                (_, __) => Task.FromResult(ErrorHandleResult.Handled),
                transactionMode);

            await SendMessage(InputQueueName);

            await messageProcessingStarted.Task;

            var pumpTask = StopPump();
            pumpStopping.SetResult();

            await pumpTask;

            Assert.False(token.IsCancellationRequested);
        }
    }
}
