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
            ErrorContext errorContext = null;

            var completed = new TaskCompletionSource<bool>();
            OnTestTimeout(() => completed.SetCanceled());

            await StartPump(
                (context, _) =>
                {
                    attempts++;
                    throw new Exception("Simulated exception");
                },
                (context, _) =>
                {
                    if (attempts == maxAttempts)
                    {
                        errorContext = context;
                        return Task.FromResult(ErrorHandleResult.Handled);
                    }

                    return Task.FromResult(ErrorHandleResult.RetryRequired);
                },
                (_, __) => attempts == maxAttempts ? completed.SetCompleted() : Task.CompletedTask,
                transactionMode);

            await SendMessage(InputQueueName);

            _ = await completed.Task;

            Assert.AreEqual(attempts, errorContext.ImmediateProcessingFailures, "Should track delivery attempts between immediate retries");
        }
    }
}
