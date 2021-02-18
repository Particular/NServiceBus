namespace NServiceBus.TransportTests
{
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
            var messageProcessingStarted = new TaskCompletionSource<bool>();
            var unpauseHandler = new TaskCompletionSource<bool>();
            var messageProcessingCompleted = new TaskCompletionSource<bool>();

            OnTestTimeout(() => messageProcessingStarted.SetCanceled());

            await StartPump(
                async (_, cancellationToken) =>
                {
                    messageProcessingStarted.SetResult(true);
                    await unpauseHandler.Task;
                },
                (_, __) => Task.FromResult(ErrorHandleResult.Handled),
                transactionMode,
                onComplete: (context, __) =>
                {
                    messageProcessingCompleted.SetResult(context.Successful);
                    return Task.CompletedTask;
                });

            await SendMessage(InputQueueName);

            _ = await messageProcessingStarted.Task;

            var stopPumpTask = StopPump(default);

            unpauseHandler.SetResult(true);

            await stopPumpTask;

            Assert.True(await messageProcessingCompleted.Task);
        }
    }
}
