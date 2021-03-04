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
                        await completed.SetCompleted();
                    }
                },
                (_, __) => Task.FromResult(ErrorHandleResult.Discarded),
                (context, _) => Task.CompletedTask,
                transactionMode);

            await SendMessage(InputQueueName);

            _ = await started.Task;

            await StopPump(new CancellationToken(true));

            _ = await completed.Task;
        }
    }
}
