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
            var started = new TaskCompletionSource();
            var cancelled = new TaskCompletionSource();

            OnTestTimeout(() =>
            {
                started.SetCanceled();
                cancelled.SetCanceled();
            });

            await StartPump(
                async (_, cancellationToken) =>
                {
                    started.SetResult();

                    try
                    {
                        await Task.Delay(TestTimeout, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        cancelled.SetResult();
                        throw;
                    }
                },
                (_, __) => Task.FromResult(ErrorHandleResult.Handled),
                transactionMode);

            await SendMessage(InputQueueName);

            await started.Task;

            await StopPump(new CancellationToken(true));

            await cancelled.Task;
        }
    }
}
