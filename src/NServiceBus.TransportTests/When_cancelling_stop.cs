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
        public async Task Should_flag_message_token_and_wait_for_completion(TransportTransactionMode transactionMode)
        {
            var onMessageCancelled = new TaskCompletionSource<bool>();
            var abortPumpStop = new CancellationTokenSource();

            OnTestTimeout(() => onMessageCancelled.SetCanceled());

            await StartPump(
                async (context, token) =>
                {
                    try
                    {
                        abortPumpStop.Cancel();
                        await Task.Delay(TestTimeout, token);
                    }
                    catch (OperationCanceledException)
                    {
                        onMessageCancelled.SetResult(true);
                    }
                },
                context =>
                {
                    return Task.FromResult(ErrorHandleResult.Handled);
                },
                transactionMode
                );

            await SendMessage(InputQueueName);

            await StopPump(abortPumpStop.Token);

            var processingWasCancelled = await onMessageCancelled.Task;

            Assert.True(processingWasCancelled);
        }
    }
}
