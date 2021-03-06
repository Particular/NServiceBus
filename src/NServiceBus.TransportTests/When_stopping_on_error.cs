namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_stopping_on_error : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_complete(TransportTransactionMode transactionMode)
        {
            CancellationToken onErrorToken = default;

            var onErrorStarted = CreateTaskCompletionSource();

            var pumpStopping = CreateTaskCompletionSource();

            await StartPump(
                (_, __) => throw new Exception(),
                async (_, cancellationToken) =>
                {
                    onErrorStarted.SetResult();
                    await pumpStopping.Task;
                    onErrorToken = cancellationToken;
                    return ErrorHandleResult.Handled;
                },
                transactionMode);

            await SendMessage(InputQueueName);

            await onErrorStarted.Task;

            var pumpTask = StopPump();
            pumpStopping.SetResult();

            await pumpTask;

            Assert.False(onErrorToken.IsCancellationRequested);
        }
    }
}
