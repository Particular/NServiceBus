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
            var completed = new TaskCompletionSource<bool>();
            OnTestTimeout(() => completed.SetCanceled());

            var retrying = false;
            var retried = true;

            await StartPump(
                (context, _) =>
                {
                    if (retrying)
                    {
                        retried = true;
                        return Task.CompletedTask;
                    }

                    throw new Exception("Simulated exception");
                },
                (context, _) =>
                {
                    retrying = true;
                    return Task.FromResult(ErrorHandleResult.RetryRequired);
                },
                (_, __) => retried ? completed.SetCompleted() : Task.CompletedTask,
                transactionMode);

            await SendMessage(InputQueueName);

            _ = await completed.Task;
        }
    }
}
