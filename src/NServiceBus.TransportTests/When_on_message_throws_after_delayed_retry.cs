namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_on_message_throws_after_delayed_retry : NServiceBusTransportTest
    {
        //[TestCase(TransportTransactionMode.None)] - not relevant
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_reset_delivery_counter(TransportTransactionMode transactionMode)
        {
            var onErrorInvoked = new TaskCompletionSource<ErrorContext>();

            OnTestTimeout(() => onErrorInvoked.SetCanceled());

            var numberOfOnErrorInvocations = 0;

            await StartPump(
                context =>
                {
                    throw new Exception("Simulated exception");
                },
                async context =>
                {
                    numberOfOnErrorInvocations += 1;

                    if (numberOfOnErrorInvocations == 1)
                    {
                        await SendMessage(InputQueueName, context.Message.Headers, context.TransportTransaction);
                    }
                    else
                    {
                        onErrorInvoked.SetResult(context);
                    }

                    return ErrorHandleResult.Handled;
                }, transactionMode);

            await SendMessage(InputQueueName, new Dictionary<string, string> { { "MyHeader", "MyValue" } });

            var errorContext = await onErrorInvoked.Task;

            Assert.AreEqual(1, errorContext.ImmediateProcessingFailures, "Should track delivery attempts between immediate retries");
        }
    }
}