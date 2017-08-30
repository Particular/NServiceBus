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

            OnTestTimeout(() =>
            {
                TestContext.Out.WriteLine("OnTestTimeout Log Output");
                onMessageCalled.SetCanceled();
            });

            await StartPump(async context =>
                {
                    TestContext.Out.WriteLine($"Start Pump header: {context.Headers.ContainsKey("IsolatedSend")}");
                    if (context.Headers.ContainsKey("IsolatedSend"))
                    {
                        onMessageCalled.SetResult(true);
                        return;
                    }

                    TestContext.Out.WriteLine("Sending Message");
                    await SendMessage(InputQueueName, new Dictionary<string, string>
                    {
                        {"IsolatedSend", "true"}
                    }, context.TransportTransaction, null, DispatchConsistency.Isolated);

                    throw new Exception("Simulated exception");
                },
                errorContext =>
                {
                    return Task.FromResult(ErrorHandleResult.Handled);
                },
                transactionMode);

            await SendMessage(InputQueueName);

            Assert.True(await onMessageCalled.Task, "Should emit isolated sends");
        }
    }
}