namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    // When an operation cancelled exception is thrown
    // while a message is being handled
    // which is not because of message processing being cancelled
    // (i.e. the endpoint shutting down)
    public class When_op_cancelled_on_message : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_invoke_recoverability(TransportTransactionMode transactionMode)
        {
            var onCompleteCalled = new TaskCompletionSource<CompleteContext>();
            var recoverabilityInvoked = false;

            OnTestTimeout(() => onCompleteCalled.SetCanceled());

            await StartPump(
                (context, _) =>
                {
                    throw new OperationCanceledException();
                },
                (_, __) =>
                {
                    recoverabilityInvoked = true;

                    return Task.FromResult(ErrorHandleResult.Handled);
                },
                transactionMode,
                onComplete: (context, _) =>
                {
                    onCompleteCalled.SetResult(context);
                    return Task.CompletedTask;
                });

            await SendMessage(InputQueueName);

            var onCompleteContext = await onCompleteCalled.Task;

            await StopPump();
            Assert.True(recoverabilityInvoked);
            Assert.True(onCompleteContext.WasAcknowledged);
        }
    }
}
