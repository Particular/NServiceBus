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
            var messageDispatched = CreateTaskCompletionSource();

            await StartPump(
                (context, _) => context.Headers.ContainsKey("FromOnError") ? messageDispatched.SetCompleted() : throw new Exception("Simulated exception"),
                async (context, cancellationToken) =>
                {
                    await SendMessage(InputQueueName, new Dictionary<string, string> { { "FromOnError", "true" } }, context.TransportTransaction, cancellationToken: cancellationToken);
                    return ErrorHandleResult.Handled;
                },
                transactionMode);

            await SendMessage(InputQueueName);

            await messageDispatched.Task;
        }
    }
}
