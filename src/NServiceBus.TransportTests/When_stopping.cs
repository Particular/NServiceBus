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
            var pumpStopping = new TaskCompletionSource<bool>();
            var onCompleteCalled = new TaskCompletionSource<CompleteContext>();

            OnTestTimeout(() =>
            {
                messageProcessingStarted.SetCanceled();
                onCompleteCalled.SetCanceled();
            });

            await StartPump(
                async (_, cancellationToken) =>
                {
                    messageProcessingStarted.SetResult(true);

                    await pumpStopping.Task;
                },
                (_, __) =>
                {
                    Assert.Fail("Recoverability should not have been invoked.");
                    return Task.FromResult(ErrorHandleResult.Handled);
                },
                transactionMode,
                onComplete: (context, _) =>
                {
                    onCompleteCalled.SetResult(context);
                    return Task.CompletedTask;
                });

            await SendMessage(InputQueueName);

            _ = await messageProcessingStarted.Task;

            var pumpTask = StopPump();
            pumpStopping.SetResult(true);

            await pumpTask;

            var completeContext = await onCompleteCalled.Task;

            Assert.True(completeContext.WasAcknowledged);
        }
    }
}
