namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class When_op_cancelled_on_error : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_invoke_critical_error(TransportTransactionMode transactionMode)
        {
            var recoverabilityStarted = new TaskCompletionSource<bool>();
            var criticalErrorInvoked = false;

            OnTestTimeout(() => recoverabilityStarted.SetCanceled());

            await StartPump(
                (_, __) => throw new Exception(),
                (_, cancellationToken) =>
                {
                    recoverabilityStarted.SetResult(true);

                    throw new OperationCanceledException();
                },
                transactionMode,
                (_, __, ___) => criticalErrorInvoked = true);

            await SendMessage(InputQueueName);

            _ = await recoverabilityStarted.Task;

            await Task.Delay(TimeSpan.FromSeconds(1));

            await StopPump(default);

            Assert.True(criticalErrorInvoked);
        }
    }
}
