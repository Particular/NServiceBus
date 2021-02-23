namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_cancelling_stop : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_cancel_message_processing(TransportTransactionMode transactionMode)
        {
            var messageProcessingStarted = new TaskCompletionSource<bool>();
            var messageProcessingCancelled = new TaskCompletionSource<bool>();
            var onCompleteCalled = new TaskCompletionSource<CompleteContext>();

            OnTestTimeout(() =>
            {
                messageProcessingStarted.SetCanceled();
                messageProcessingCancelled.SetCanceled();
                onCompleteCalled.SetCanceled();
            });

            await StartPump(
                async (_, cancellationToken) =>
                {
                    messageProcessingStarted.SetResult(true);

                    try
                    {
                        await Task.Delay(TestTimeout, cancellationToken);
                        messageProcessingCancelled.SetResult(false);
                    }
                    catch (OperationCanceledException)
                    {
                        messageProcessingCancelled.SetResult(true);
                        // Still need the pump to get the exception or the message will be ACK'ed
                        throw;
                    }
                },
                (_, __) => Task.FromResult(ErrorHandleResult.Handled),
                transactionMode,
                onComplete: (context, _) =>
                {
                    onCompleteCalled.SetResult(context);
                    return Task.CompletedTask;
                });

            await SendMessage(InputQueueName);

            _ = await messageProcessingStarted.Task;

            await StopPump(new CancellationToken(true));

            var wasCancelled = await messageProcessingCancelled.Task;

            var completeContext = await onCompleteCalled.Task;

            Assert.True(wasCancelled);
            Assert.False(completeContext.WasAcknowledged);
        }
    }
}
