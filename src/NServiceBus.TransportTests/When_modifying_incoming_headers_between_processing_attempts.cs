namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_modifying_incoming_headers_between_processing_attempts : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_roll_back_header_modifications_between_processing_attempts(
            TransportTransactionMode transactionMode)
        {
            var messageRetries = new TaskCompletionSource<MessageContext>();
            var firstInvocation = true;

            OnTestTimeout(() => messageRetries.SetCanceled());

            await StartPump(context =>
                {
                    if (firstInvocation)
                    {
                        context.Headers["test-header"] = "modified";
                        firstInvocation = false;
                        throw new Exception();
                    }

                    messageRetries.SetResult(context);
                    return Task.FromResult(0);
                },
                context => Task.FromResult(ErrorHandleResult.RetryRequired),
                transactionMode);

            await SendMessage(InputQueueName, new Dictionary<string, string> { { "test-header", "original" } });

            var retriedMessage = await messageRetries.Task;

            Assert.AreEqual("original", retriedMessage.Headers["test-header"]);
        }
    }
}