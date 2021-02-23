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
            var recoverabilityStarted = new TaskCompletionSource<bool>();
            var onCompleteCalled = new TaskCompletionSource<CompleteContext>();

            OnTestTimeout(() =>
            {
                recoverabilityStarted.SetCanceled();
                onCompleteCalled.SetCanceled();
            });

            await StartPump(
                (_, __) => throw new Exception(),
                async (_, cancellationToken) =>
                {
                    recoverabilityStarted.SetResult(true);

                    await Task.Delay(TestTimeout, cancellationToken);

                    return ErrorHandleResult.Handled;
                },
                transactionMode,
                (_, __, ___) => Assert.Fail("Critical error should not be invoked"),
                onComplete: (context, _) =>
                {
                    onCompleteCalled.SetResult(context);
                    return Task.CompletedTask;
                });

            await SendMessage(InputQueueName);

            _ = await recoverabilityStarted.Task;

            await StopPump(new CancellationToken(true));

            var completeContext = await onCompleteCalled.Task;

            Assert.False(completeContext.WasAcknowledged);
        }
    }
}
