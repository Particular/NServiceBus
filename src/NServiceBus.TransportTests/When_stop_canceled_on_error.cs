namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_stop_canceled_on_error : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_not_invoke_critical_error(TransportTransactionMode transactionMode)
        {
            var criticalErrorInvoked = false;

            var onErrorStarted = CreateTaskCompletionSource();

            await StartPump(
                (_, __) => throw new Exception(),
                async (_, cancellationToken) =>
                {
                    onErrorStarted.SetResult();

                    await Task.Delay(TestTimeout, cancellationToken);

                    return ErrorHandleResult.Handled;
                },
                transactionMode,
                (_, __, ___) => criticalErrorInvoked = true);

            await SendMessage(InputQueueName);

            await onErrorStarted.Task;

            await StopPump(new CancellationToken(true));

            Assert.False(criticalErrorInvoked, "Critical error should not be invoked");
        }
    }
}
