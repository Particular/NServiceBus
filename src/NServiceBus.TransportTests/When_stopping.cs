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
                        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        messageProcessingCancelled.SetResult(true);
                    }

                    messageProcessingCancelled.SetResult(false);
                },
                (_, __) => Task.FromResult(ErrorHandleResult.Handled),
                transactionMode);

            await SendMessage(InputQueueName);

            _ = await messageProcessingStarted.Task;

            await StopPump(default);

            Assert.False(await messageProcessingCancelled.Task);
        }
    }
}
