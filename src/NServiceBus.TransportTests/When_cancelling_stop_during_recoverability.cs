namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_cancelling_stop_during_recoverability : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_not_invoke_critical_error(TransportTransactionMode transactionMode)
        {
            var recoverabilityStarted = new TaskCompletionSource<bool>();
            var criticalErrorInvoked = false;

            OnTestTimeout(() => recoverabilityStarted.SetCanceled());

            await StartPump(
                (_, __) => throw new Exception(),
                async (_, cancellationToken) =>
                {
                    recoverabilityStarted.SetResult(true);

                    await Task.Delay(TestTimeout, cancellationToken);

                    return ErrorHandleResult.Handled;
                },
                transactionMode,
                (_, __, ___) => criticalErrorInvoked = true);

            await SendMessage(InputQueueName);

            _ = await recoverabilityStarted.Task;

            await StopPump(new CancellationToken(true));

            Assert.False(criticalErrorInvoked);
        }
    }
}
