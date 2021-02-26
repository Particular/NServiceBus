namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_on_message_throws_after_delayed_retry : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_reset_delivery_counter(TransportTransactionMode transactionMode)
        {
            ErrorContext errorContext = null;

            var completed = new TaskCompletionSource<bool>();
            OnTestTimeout(() => completed.SetCanceled());

            var sendingDelayedMessage = false;
            var sentDelayedMessage = false;

            await StartPump(
                (context, _) => throw new Exception("Simulated exception"),
                async (context, _) =>
                {
                    if (!sendingDelayedMessage)
                    {
                        sendingDelayedMessage = true;
                        await SendMessage(InputQueueName, context.Message.Headers, context.TransportTransaction);
                    }
                    else
                    {
                        sentDelayedMessage = true;
                        errorContext = context;
                    }

                    return ErrorHandleResult.Handled;
                },
                (_, __) => sentDelayedMessage ? completed.SetCompleted() : Task.CompletedTask,
                transactionMode);

            await SendMessage(InputQueueName);

            _ = await completed.Task;

            Assert.AreEqual(1, errorContext.ImmediateProcessingFailures, "Should track delivery attempts between immediate retries");
        }
    }
}
