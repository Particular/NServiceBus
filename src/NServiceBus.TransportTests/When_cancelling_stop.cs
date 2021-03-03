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
            var wasCancelled = false;

            var started = new TaskCompletionSource<bool>();
            var completed = new TaskCompletionSource<bool>();

            OnTestTimeout(() =>
            {
                started.SetCanceled();
                completed.SetCanceled();
            });

            await StartPump(
                async (_, cancellationToken) =>
                {
                    started.SetResult(true);

                    try
                    {
                        await Task.Delay(TestTimeout, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        wasCancelled = true;

                        // propagate the cancellation
                        // we are only catching the exception here to record the cancellation
                        throw;
                    }
                },
                (_, __) => Task.FromResult(ReceiveResult.Discarded),
                (context, _) => completed.SetCompleted(),
                transactionMode);

            await SendMessage(InputQueueName);

            _ = await started.Task;

            await StopPump(new CancellationToken(true));

            _ = await completed.Task;

            Assert.True(wasCancelled);
        }
    }
}
