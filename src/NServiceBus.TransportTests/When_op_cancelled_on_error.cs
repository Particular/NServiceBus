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
            var criticalErrorInvoked = false;

            var completed = new TaskCompletionSource<CompleteContext>();
            OnTestTimeout(() => completed.SetCanceled());

            await StartPump(
                (_, __) => throw new Exception(),
                (_, __) => throw new OperationCanceledException(),
                (context, _) => completed.SetCompleted(context),
                transactionMode,
                (_, __, ___) => criticalErrorInvoked = true);

            await SendMessage(InputQueueName);

            var completeContext = await completed.Task;

            await StopPump();
            Assert.True(criticalErrorInvoked);
            Assert.True(completeContext.OnMessageFailed);

            if (transactionMode != TransportTransactionMode.None)
            {
                Assert.False(completeContext.WasAcknowledged);
            }
        }
    }
}
