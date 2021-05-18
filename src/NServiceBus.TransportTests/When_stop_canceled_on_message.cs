namespace NServiceBus.TransportTests
{
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_stop_canceled_on_message : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_not_invoke_recoverability(TransportTransactionMode transactionMode)
        {
            var recoverabilityInvoked = false;

            var onMessageStarted = CreateTaskCompletionSource();

            await StartPump(
                async (_, cancellationToken) =>
                {
                    onMessageStarted.SetResult();
                    await Task.Delay(TestTimeout, cancellationToken);
                },
                (_, __) =>
                {
                    recoverabilityInvoked = true;
                    return Task.FromResult(ErrorHandleResult.Handled);
                },
                transactionMode);

            await SendMessage(InputQueueName);

            await onMessageStarted.Task;

            await StopPump(new CancellationToken(true));

            Assert.False(recoverabilityInvoked, "Recoverability should not have been invoked.");
        }
    }
}
