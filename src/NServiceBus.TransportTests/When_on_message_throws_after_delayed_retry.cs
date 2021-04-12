namespace NServiceBus.TransportTests
{
    using System;
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
            var sentDelayedMessage = CreateTaskCompletionSource<ErrorContext>();

            var sendingDelayedMessage = false;

            await StartPump(
                (_, __) => throw new Exception("Simulated exception"),
                async (context, cancellationToken) =>
                {
                    if (!sendingDelayedMessage)
                    {
                        sendingDelayedMessage = true;
                        await SendMessage(InputQueueName, context.Message.Headers, context.TransportTransaction, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        sentDelayedMessage.SetResult(context);
                    }

                    return ErrorHandleResult.Handled;
                },
                transactionMode);

            await SendMessage(InputQueueName);

            var errorContext = await sentDelayedMessage.Task;

            Assert.AreEqual(1, errorContext.ImmediateProcessingFailures, "Should track delivery attempts between immediate retries");
        }
    }
}
