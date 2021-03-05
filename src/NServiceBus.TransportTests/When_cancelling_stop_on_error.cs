namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_cancelling_stop_on_error : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_cancel(TransportTransactionMode transactionMode)
        {
            var started = new TaskCompletionSource();
            var cancelled = new TaskCompletionSource<bool>();

            OnTestTimeout(() =>
            {
                started.SetCanceled();
                cancelled.SetCanceled();
            });

            await StartPump(
                (_, __) => throw new Exception(),
                async (_, cancellationToken) =>
                {
                    started.SetResult();

                    try
                    {
                        await Task.Delay(TestTimeout, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        cancelled.SetResult(true);
                        throw;
                    }

                    cancelled.SetResult(false);

                    return ErrorHandleResult.Handled;
                },
                transactionMode);

            await SendMessage(InputQueueName);

            await started.Task;

            await StopPump(new CancellationToken(true));

            var wasCancelled = await cancelled.Task;

            Assert.True(wasCancelled, "onError was not properly cancelled.");
        }
    }
}