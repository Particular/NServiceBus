namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Transport;
    using NUnit.Framework;

    // When an operation cancelled exception is thrown
    // while recoverability is running
    // which is not because of message processing being cancelled
    // (i.e. the endpoint shutting down)
    public class When_op_cancelled_on_error : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_invoke_critical_error(TransportTransactionMode transactionMode)
        {
            var onCompleteCalled = new TaskCompletionSource<CompleteContext>();
            var criticalErrorInvoked = false;

            OnTestTimeout(() => onCompleteCalled.SetCanceled());

            await StartPump(
                (_, __) => throw new Exception(),
                (_, __) =>
                {
                    throw new OperationCanceledException();
                },
                transactionMode,
                (_, __, ___) => criticalErrorInvoked = true,
                onComplete: (context, _) =>
                {
                    onCompleteCalled.SetResult(context);
                    return Task.CompletedTask;
                });

            await SendMessage(InputQueueName);

            var completeContext = await onCompleteCalled.Task;

            await StopPump();
            Assert.True(criticalErrorInvoked);
            Assert.False(completeContext.WasAcknowledged);
        }
    }
}
