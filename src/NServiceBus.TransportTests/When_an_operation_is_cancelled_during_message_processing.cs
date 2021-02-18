namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_an_operation_is_cancelled_during_message_processing : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_invoke_recoverability(TransportTransactionMode transactionMode)
        {
            var messageCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var recoverabilityInvoked = false;

            OnTestTimeout(() => messageCompleted.SetCanceled());

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
                onComplete: (context, __) =>
                {
                    messageCompleted.SetResult(context.Successful);
                    return Task.CompletedTask;
                });

            await SendMessage(InputQueueName);

            var messageCompletedSuccessfully = await messageCompleted.Task;

            await StopPump(default);

            Assert.True(recoverabilityInvoked);
            Assert.True(messageCompletedSuccessfully, "Since the recoverability result is ErrorHandleResult.Handled the message should be completed successfully");
        }
    }
}
