namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_failure_happens_after_send : NServiceBusTransportTest
    {
        //[TestCase(TransportTransactionMode.None)] - not relevant
        //[TestCase(TransportTransactionMode.ReceiveOnly)] - not relevant
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_not_emit_messages(TransportTransactionMode transactionMode)
        {
            var onMessageCalled = new TaskCompletionSource<bool>();

            OnTestTimeout(() => onMessageCalled.SetCanceled());

            await StartPump(async context =>
            {
                if (context.Headers.ContainsKey("CompleteTest"))
                {
                    onMessageCalled.SetResult(true);
                    return SuccessfulMessageProcessingResult;
                }

                if (context.Headers.ContainsKey("EnlistedSend"))
                {
                    onMessageCalled.SetResult(false);
                    return SuccessfulMessageProcessingResult;
                }

                await SendMessage(InputQueueName, new Dictionary<string, string> { { "EnlistedSend", "true" } }, context.TransportTransaction);

                throw new Exception("Simulated exception");

            },
                async context =>
                {
                    await SendMessage(InputQueueName, new Dictionary<string, string> { { "CompleteTest", "true" } }, context.TransportTransaction);

                    return ErrorHandleResult.Handled;
                }, transactionMode);

            await SendMessage(InputQueueName, new Dictionary<string, string> { { "MyHeader", "MyValue" } });

            Assert.True(await onMessageCalled.Task, "Should not emit enlisted sends");
        }
    }
}