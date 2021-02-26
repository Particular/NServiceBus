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
            var onCompleteCalled = new TaskCompletionSource<CompleteContext>();
            ErrorContext errorContext = null;

            OnTestTimeout(() => onCompleteCalled.SetCanceled());

            await StartPump((context, _) =>
            {
                throw new Exception("Simulated exception");
            },
                (context, _) =>
                {
                    errorContext = context;

                    return Task.FromResult(ErrorHandleResult.Handled);
                },
                transactionMode,
                onComplete: (context, _) =>
                {
                    onCompleteCalled.SetResult(context);
                    return Task.CompletedTask;
                });

            await SendMessage(InputQueueName, new Dictionary<string, string> { { "MyHeader", "MyValue" } });

            var completeContext = await onCompleteCalled.Task;

            Assert.True(completeContext.OnMessageFailed, "Message failure should be indicated");
            Assert.NotNull(errorContext, "On error should have been called");
            Assert.AreEqual(errorContext.Exception.Message, "Simulated exception", "Should preserve the exception");
            Assert.AreEqual(1, errorContext.ImmediateProcessingFailures, "Should track the number of delivery attempts");
            Assert.AreEqual("MyValue", errorContext.Message.Headers["MyHeader"], "Should pass the message headers");
        }
    }
}