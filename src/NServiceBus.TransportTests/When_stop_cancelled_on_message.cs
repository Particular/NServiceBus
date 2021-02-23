namespace NServiceBus.TransportTests
{
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_stop_cancelled_on_message : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_not_invoke_recoverability(TransportTransactionMode transactionMode)
        {
            var messageProcessingStarted = new TaskCompletionSource<bool>();
            var onCompleteCalled = new TaskCompletionSource<CompleteContext>();

            OnTestTimeout(() =>
            {
                messageProcessingStarted.SetCanceled();
                onCompleteCalled.SetCanceled();
            });

            await StartPump(
                async (_, cancellationToken) =>
                {
                    messageProcessingStarted.SetResult(true);
                    await Task.Delay(TestTimeout, cancellationToken);
                },
                (_, __) =>
                {
                    Assert.Fail("Recoverability should not have been invoked.");

                    return Task.FromResult(ErrorHandleResult.Handled);
                },
                transactionMode,
                onComplete: (context, _) =>
                {
                    onCompleteCalled.SetResult(context);
                    return Task.CompletedTask;
                });

            await SendMessage(InputQueueName);

            _ = await messageProcessingStarted.Task;

            await StopPump(new CancellationToken(true));

            var completeContext = await onCompleteCalled.Task;

            Assert.IsFalse(completeContext.WasAcknowledged);
        }
    }
}
