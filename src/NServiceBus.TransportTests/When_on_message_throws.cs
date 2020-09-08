namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_on_message_throws : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_call_on_error(TransportTransactionMode transactionMode)
        {
            var onErrorCalled = new TaskCompletionSource<ErrorContext>();

            OnTestTimeout(() => onErrorCalled.SetCanceled());

            await StartPump((context, ct) =>
            {
                throw new Exception("Simulated exception");
            },
                (context, ct) =>
                {
                    onErrorCalled.SetResult(context);

                    return Task.FromResult(ErrorHandleResult.Handled);
                }, transactionMode);

            await SendMessage(InputQueueName, new Dictionary<string, string> { { "MyHeader", "MyValue" } });

            var errorContext = await onErrorCalled.Task;

            Assert.AreEqual(errorContext.Exception.Message, "Simulated exception", "Should preserve the exception");
            Assert.AreEqual(1, errorContext.ImmediateProcessingFailures, "Should track the number of delivery attempts");
            Assert.AreEqual("MyValue", errorContext.Message.Headers["MyHeader"], "Should pass the message headers");
        }
    }
}