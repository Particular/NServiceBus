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
        [Explicit("There is a race condition between the OperationCanceledException being thrown and stopping the pump")]
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_invoke_recoverability(TransportTransactionMode transactionMode)
        {
            var messageProcessingStarted = new TaskCompletionSource<bool>();
            var recoverabilityInvoked = false;

            OnTestTimeout(() => messageProcessingStarted.SetCanceled());

            await StartPump(
                (context, _) =>
                {
                    messageProcessingStarted.SetResult(true);

                    throw new OperationCanceledException();
                },
                (_, __) =>
                {
                    recoverabilityInvoked = true;

                    return Task.FromResult(ErrorHandleResult.Handled);
                },
                transactionMode);

            await SendMessage(InputQueueName);

            _ = await messageProcessingStarted.Task;

            await Task.Delay(TimeSpan.FromSeconds(1));

            await StopPump();

            Assert.True(recoverabilityInvoked);
        }
    }
}
