namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_failure_happens_after_isolated_send : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_emit_messages(TransportTransactionMode transactionMode)
        {
            var onMessageCalled = new TaskCompletionSource<bool>();

            OnTestTimeout(() => onMessageCalled.SetCanceled());

            await StartPump(async context =>
                {
                    if (context.Headers.ContainsKey("CompleteTest"))
                    {
                        onMessageCalled.SetResult(true);
                        return;
                    }

                    if (context.Headers.ContainsKey("EnlistedSend"))
                    {
                        onMessageCalled.SetResult(false);
                        return;
                    }

                    await SendMessage(InputQueueName, new Dictionary<string, string>
                    {
                        {"EnlistedSend", "true"}
                    }, context.TransportTransaction, null, DispatchConsistency.Isolated);

                    throw new Exception("Simulated exception");
                },
                async context =>
                {
                    await SendMessage(InputQueueName, new Dictionary<string, string>
                    {
                        {"CompleteTest", "true"}
                    }, context.TransportTransaction, null, DispatchConsistency.Isolated);

                    return ErrorHandleResult.Handled;
                }, transactionMode);

            await SendMessage(InputQueueName, new Dictionary<string, string>
            {
                {"MyHeader", "MyValue"}
            });

            Assert.False(await onMessageCalled.Task, "Should emit enlisted sends");
        }
    }
}