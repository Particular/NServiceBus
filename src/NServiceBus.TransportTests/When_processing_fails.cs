namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_processing_fails : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_float_context_to_error(TransportTransactionMode transactionMode)
        {
            var onErrorCalled = new TaskCompletionSource<ErrorContext>();

            OnTestTimeout(() => onErrorCalled.SetCanceled());

            await StartPump((context, _) =>
            {
                context.Extensions.Set("MyKey", "MyValue");

                throw new Exception("Simulated exception");
            },
            (context, _) =>
            {
                onErrorCalled.SetResult(context);

                return Task.FromResult(ErrorHandleResult.Handled);
            }, transactionMode);

            await SendMessage(InputQueueName, new Dictionary<string, string>());

            var errorContext = await onErrorCalled.Task;

            Assert.AreEqual("MyValue", errorContext.Extensions.Get<string>("MyKey"));
        }
    }
}