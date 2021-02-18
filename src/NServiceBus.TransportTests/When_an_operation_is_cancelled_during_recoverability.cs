namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class When_an_operation_is_cancelled_during_recoverability : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_invoke_critical_error(TransportTransactionMode transactionMode)
        {
            var messageCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var criticalErrorInvoked = false;

            OnTestTimeout(() => messageCompleted.SetCanceled());

            await StartPump(
                (_, __) => throw new Exception(),
                (_, cancellationToken) =>
                {
                    throw new OperationCanceledException();
                },
                transactionMode,
                (_, __, ___) => criticalErrorInvoked = true,
                (_, __) =>
                {
                    messageCompleted.SetResult(true);

                    return Task.CompletedTask;
                });

            await SendMessage(InputQueueName);

            _ = await messageCompleted.Task;


            await StopPump(default);

            Assert.True(criticalErrorInvoked);
        }
    }
}
