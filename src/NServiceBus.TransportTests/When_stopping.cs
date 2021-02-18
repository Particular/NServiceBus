namespace NServiceBus.TransportTests
{
    using System;
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
            var messageProcessingCancelled = new TaskCompletionSource<bool>();
            var pumpAskedToStop = new TaskCompletionSource<bool>();
            var messageProcessingCompleted = new TaskCompletionSource<bool>();

            OnTestTimeout(() =>
            {
                messageProcessingStarted.SetCanceled();
                messageProcessingCancelled.SetCanceled();
            });

            await StartPump(
                async (_, cancellationToken) =>
                {
                    messageProcessingStarted.SetResult(true);

                    try
                    {
                        await pumpAskedToStop.Task;
                    }
                    catch (OperationCanceledException)
                    {
                        messageProcessingCancelled.SetResult(true);
                    }

                    messageProcessingCancelled.SetResult(false);
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

            pumpAskedToStop.SetResult(true);

            await Task.WhenAll(stopPumpTask, messageProcessingCompleted.Task);


            Assert.False(await messageProcessingCancelled.Task);
        }
    }
}
