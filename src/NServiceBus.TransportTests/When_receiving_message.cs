namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_receiving_message : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_float_context(TransportTransactionMode transactionMode)
        {
            var onError = CreateTaskCompletionSource<ErrorContext>();

            await StartPump(
                (context, _) =>
                {
                    context.Extensions.Set("MyKey", "MyValue");
                    throw new Exception("Simulated exception");
                },
                (context, _) =>
                {
                    onError.SetResult(context);
                    return Task.FromResult(ErrorHandleResult.Handled);
                },
                transactionMode);

            await SendMessage(InputQueueName);

            var errorContext = await onError.Task;

            Assert.AreEqual("MyValue", errorContext.Extensions.Get<string>("MyKey"));
        }
    }
}
