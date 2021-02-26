namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
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

            var completed = new TaskCompletionSource<bool>();
            OnTestTimeout(() => completed.SetCanceled());

            await StartPump(
                async (context, _) =>
                {
                    if (context.Headers.ContainsKey("SentBeforeFailure"))
                    {
                        messageEmitted = true;
                        return;
                    }

                    await SendMessage(InputQueueName, new Dictionary<string, string> { { "SentBeforeFailure", "" } }, context.TransportTransaction);

                    throw new Exception("Simulated exception");

                },
                (errorContext, __) => Task.FromResult(ErrorHandleResult.Handled),
                (context, __) => completed.SetCompleted(),
                transactionMode);

            await SendMessage(InputQueueName);

            _ = await completed.Task;

            await StopPump(CancellationToken.None);

            Assert.False(messageEmitted);
        }
    }
}
