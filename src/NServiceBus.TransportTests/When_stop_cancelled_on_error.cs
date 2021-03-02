namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_stop_cancelled_on_error : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None, true)]
        [TestCase(TransportTransactionMode.ReceiveOnly, false)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive, false)]
        [TestCase(TransportTransactionMode.TransactionScope, false)]
        public async Task Should_not_invoke_critical_error(TransportTransactionMode transactionMode, bool acknowledgementExpected)
        {
            var criticalErrorInvoked = false;

            var recoverabilityStarted = new TaskCompletionSource<bool>();
            var completed = new TaskCompletionSource<CompleteContext>();

            OnTestTimeout(() =>
            {
                recoverabilityStarted.SetCanceled();
                completed.SetCanceled();
            });

            await StartPump(
                (_, __) => throw new Exception(),
                async (_, cancellationToken) =>
                {
                    recoverabilityStarted.SetResult(true);

                    await Task.Delay(TestTimeout, cancellationToken);

                    return ErrorHandleResult.Handled;
                },
                (context, _) => completed.SetCompleted(context),
                transactionMode,
                (_, __, ___) => criticalErrorInvoked = true);

            await SendMessage(InputQueueName);

            _ = await recoverabilityStarted.Task;

            await StopPump(new CancellationToken(true));

            var completeContext = await completed.Task;

            Assert.False(criticalErrorInvoked, "Critical error should not be invoked");
            Assert.True(completeContext.OnMessageFailed);
            Assert.AreEqual(acknowledgementExpected, completeContext.WasAcknowledged);
        }
    }
}
