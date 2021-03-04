namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_stop_cancelled_on_error : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_not_invoke_critical_error(TransportTransactionMode transactionMode)
        {
            var criticalErrorInvoked = false;

            var recoverabilityStarted = new TaskCompletionSource<bool>();
            OnTestTimeout(() => recoverabilityStarted.SetCanceled());

            await StartPump(
                (_, __) => throw new Exception(),
                async (_, cancellationToken) =>
                {
                    recoverabilityStarted.SetResult(true);

                    await Task.Delay(TestTimeout, cancellationToken);

                    return ErrorHandleResult.Discarded;
                },
                (_, __) => Task.CompletedTask,
                transactionMode,
                (_, __, ___) => criticalErrorInvoked = true);

            await SendMessage(InputQueueName);

            _ = await recoverabilityStarted.Task;

            await StopPump(new CancellationToken(true));

            Assert.False(criticalErrorInvoked, "Critical error should not be invoked");
        }
    }
}
