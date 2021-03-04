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
            var recoverability = new TaskCompletionSource<bool>();
            OnTestTimeout(() => recoverability.SetCanceled());

            await StartPump(
                (_, __) => throw new OperationCanceledException(),
                (_, __) =>
                {
                    recoverability.SetResult(true);
                    return Task.FromResult(ErrorHandleResult.Handled);
                },
                transactionMode);

            await SendMessage(InputQueueName);

            _ = await recoverability.Task;
        }
    }
}
