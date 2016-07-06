namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class When_on_error_throws : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_invoke_critical_error(TransportTransactionMode transactionMode)
        {
            var onCriticalErrorCalled = new TaskCompletionSource<Exception>();

            OnTestTimeout(() => onCriticalErrorCalled.SetResult(null));

            await StartPump(context =>
            {
                throw new Exception("Simulated exception");
            },
                context =>
                {
                    throw new Exception("Simulated exception from on error");
                }, transactionMode,
                (m, ex) =>
                {
                    onCriticalErrorCalled.SetResult(ex);
                    return Task.FromResult(0);
                });

            await SendMessage(InputQueueName);

            var exReceived = await onCriticalErrorCalled.Task;

            Assert.AreEqual(exReceived.Message, "Simulated exception from on error");
        }
    }
}