namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_on_message_throws_after_immediate_retry : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_persist_message_delivery_count_between_on_error_calls(TransportTransactionMode transactionMode)
        {
            var maxAttempts = 3;
            var attempts = 0;

            var maxAttemptsReached = CreateTaskCompletionSource<ErrorContext>();

            await StartPump(
                (_, __) => throw new Exception($"Simulated exception {++attempts}"),
                (context, _) =>
                {
                    if (attempts == maxAttempts)
                    {
                        maxAttemptsReached.SetResult(context);
                        return Task.FromResult(ErrorHandleResult.Handled);
                    }

                    return Task.FromResult(ErrorHandleResult.RetryRequired);
                },
                transactionMode);

            await SendMessage(InputQueueName);

            var errorContext = await maxAttemptsReached.Task;

            Assert.AreEqual(attempts, errorContext.ImmediateProcessingFailures, "Should track delivery attempts between immediate retries");
        }
    }
}
