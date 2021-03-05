namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_failure_happens_after_send : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_not_emit_messages(TransportTransactionMode transactionMode)
        {
            var messageEmitted = false;

            var sentFromErrorReceived = new TaskCompletionSource();
            OnTestTimeout(() => sentFromErrorReceived.SetCanceled());

            await StartPump(
                async (context, _) =>
                {
                    if (context.Headers.ContainsKey("SentFromOnError"))
                    {
                        sentFromErrorReceived.SetResult();
                        return;
                    }

                    if (context.Headers.ContainsKey("SentBeforeFailure"))
                    {
                        messageEmitted = true;
                        return;
                    }

                    await SendMessage(InputQueueName, new Dictionary<string, string> { { "SentBeforeFailure", "" } }, context.TransportTransaction);

                    throw new Exception("Simulated exception");

                },
                async (context, _) =>
                {
                    await SendMessage(InputQueueName, new Dictionary<string, string> { { "SentFromOnError", "" } }, context.TransportTransaction);
                    return ErrorHandleResult.Handled;
                },
                transactionMode);

            await SendMessage(InputQueueName);

            await sentFromErrorReceived.Task;

            await StopPump();

            Assert.False(messageEmitted);
        }
    }
}
