namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_requesting_immediate_retry : NServiceBusTransportTest
    {
        //[TestCase(TransportTransactionMode.None)]  - Currently not supported, but there are plans to change that: https://github.com/Particular/NServiceBus/issues/2750
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_retry_immediately(TransportTransactionMode transactionMode)
        {
            var retried = CreateTaskCompletionSource();

            var retrying = false;

            await StartPump(
                (_, __) => retrying ? retried.SetCompleted() : throw new Exception("Simulated exception"),
                (_, __) =>
                {
                    retrying = true;
                    return Task.FromResult(ErrorHandleResult.RetryRequired);
                },
                transactionMode);

            await SendMessage(InputQueueName);

            await retried.Task;
        }
    }
}
