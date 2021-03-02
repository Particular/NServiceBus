namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_cancelling_stop : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None, true)]
        [TestCase(TransportTransactionMode.ReceiveOnly, false)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive, false)]
        [TestCase(TransportTransactionMode.TransactionScope, false)]
        public async Task Should_cancel_message_processing(TransportTransactionMode transactionMode, bool acknowledgementExpected)
        {
            var wasCancelled = false;

            var started = new TaskCompletionSource<bool>();
            var completed = new TaskCompletionSource<CompleteContext>();

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
                (_, __) => Task.FromResult(ErrorHandleResult.Handled),
                (context, _) => completed.SetCompleted(context),
                transactionMode);

            await SendMessage(InputQueueName);

            _ = await started.Task;

            await StopPump(new CancellationToken(true));

            var completeContext = await completed.Task;

            Assert.True(wasCancelled);
            Assert.False(completeContext.OnMessageFailed);
            Assert.AreEqual(acknowledgementExpected, completeContext.WasAcknowledged);
        }
    }
}
