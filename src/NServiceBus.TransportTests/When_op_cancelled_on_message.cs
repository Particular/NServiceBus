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
            var recoverabilityInvoked = false;

            var completed = new TaskCompletionSource<bool>();
            OnTestTimeout(() => completed.SetCanceled());

            await StartPump(
                (_, __) => throw new OperationCanceledException(),
                (_, __) =>
                {
                    recoverabilityInvoked = true;
                    return Task.FromResult(ReceiveResult.Discarded);
                },
                (_, __) => completed.SetCompleted(),
                transactionMode);

            await SendMessage(InputQueueName);

            _ = await completed.Task;

            await StopPump();

            Assert.True(recoverabilityInvoked);
        }
    }
}
