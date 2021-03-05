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
            var messageEmitted = new TaskCompletionSource();
            OnTestTimeout(() => messageEmitted.SetCanceled());

            await StartPump(
                async (context, _) =>
                {
                    if (context.Headers.ContainsKey("IsolatedSend"))
                    {
                        messageEmitted.SetResult();
                        return;
                    }

                    await SendMessage(
                        InputQueueName,
                        new Dictionary<string, string> { { "IsolatedSend", "" } },
                        context.TransportTransaction,
                        dispatchConsistency: DispatchConsistency.Isolated);

                    throw new Exception("Simulated exception");
                },
                (_, __) => Task.FromResult(ErrorHandleResult.Handled),
                transactionMode);

            await SendMessage(InputQueueName);

            await messageEmitted.Task;
        }
    }
}
