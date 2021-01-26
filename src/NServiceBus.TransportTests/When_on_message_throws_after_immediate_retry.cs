namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_on_message_throws_after_immediate_retry : NServiceBusTransportTest
    {
        //[TestCase(TransportTransactionMode.None)] - not relevant
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_persist_message_delivery_count_between_on_error_calls(TransportTransactionMode transactionMode)
        {
            var onErrorInvoked = new TaskCompletionSource<ErrorContext>();

            OnTestTimeout(() => onErrorInvoked.SetCanceled());

            var numberOfOnErrorInvocations = 0;

            await StartPump(
                (context, _) =>
                {
                    throw new Exception("Simulated exception");
                },
                (context, _) =>
                {
                    numberOfOnErrorInvocations += 1;

                    if (numberOfOnErrorInvocations == 3)
                    {
                        onErrorInvoked.SetResult(context);

                        return Task.FromResult(ErrorHandleResult.Handled);
                    }

                    return Task.FromResult(ErrorHandleResult.RetryRequired);
                }, transactionMode);

            await SendMessage(InputQueueName, new Dictionary<string, string> { { "MyHeader", "MyValue" } });

            var errorContext = await onErrorInvoked.Task;

            Assert.AreEqual(numberOfOnErrorInvocations, errorContext.ImmediateProcessingFailures, "Should track delivery attempts between immediate retries");
        }
    }
}