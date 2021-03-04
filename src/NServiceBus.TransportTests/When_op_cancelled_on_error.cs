namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;
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
            var criticalError = new TaskCompletionSource<bool>();
            OnTestTimeout(() => criticalError.SetCanceled());

            await StartPump(
                (_, __) => throw new Exception(),
                (_, __) => throw new OperationCanceledException(),
                transactionMode,
                (_, __, ___) => criticalError.SetResult(true));

            await SendMessage(InputQueueName);

            _ = await criticalError.Task;
        }
    }
}
