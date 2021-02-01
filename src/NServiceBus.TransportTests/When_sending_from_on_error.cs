namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_sending_from_on_error : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_dispatch_the_message(TransportTransactionMode transactionMode)
        {
            var messageReceived = new TaskCompletionSource<bool>();

            OnTestTimeout(() => messageReceived.SetResult(false));

            await StartPump(
                context =>
                {
                    if (context.Headers.ContainsKey("FromOnError"))
                    {
                        messageReceived.SetResult(true);
                        return Task.FromResult(SuccessfulMessageProcessingResult);
                    }

                    throw new Exception("Simulated exception");
                },
                async context =>
                {
                    await SendMessage(InputQueueName, new Dictionary<string, string> { { "FromOnError", "true" } }, context.TransportTransaction);

                    return ErrorHandleResult.Handled;
                }, transactionMode);

            await SendMessage(InputQueueName);

            Assert.True(await messageReceived.Task, "Message not dispatched properly");
        }
    }
}