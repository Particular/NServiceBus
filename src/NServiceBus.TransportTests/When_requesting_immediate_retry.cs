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
            var messageRetried = new TaskCompletionSource<bool>();

            OnTestTimeout(() => messageRetried.SetResult(false));

            var hasBeenCalled = false;

            await StartPump(
                context =>
                {
                    if (hasBeenCalled)
                    {
                        messageRetried.SetResult(true);
                        return Task.FromResult(SuccessfulMessageProcessingResult);
                    }
                    hasBeenCalled = true;
                    throw new Exception("Simulated exception");
                },
                context => Task.FromResult(ErrorHandleResult.RetryRequired), transactionMode);

            await SendMessage(InputQueueName);

            Assert.True(await messageRetried.Task, "Should retry if asked so");
        }
    }
}