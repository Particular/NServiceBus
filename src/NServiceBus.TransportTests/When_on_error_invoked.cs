using System;
using System.Threading.Tasks;
using NServiceBus.Transport;
using NUnit.Framework;

namespace NServiceBus.TransportTests
{
    public class When_on_error_invoked : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_share_extensions_context_with_message_context(TransportTransactionMode transactionMode)
        {
            var messageContextSource = CreateTaskCompletionSource<MessageContext>();
            var errorContextSource = CreateTaskCompletionSource<ErrorContext>();
            
            await StartPump((messageContext, _) =>
            {
                messageContextSource.SetCompleted(messageContext);
                throw new Exception("trigger onError");
            }, (errorContext, _) =>
            {
                errorContextSource.SetCompleted(errorContext);
                return Task.FromResult(ErrorHandleResult.Handled);
            }, transactionMode);

            await SendMessage(InputQueueName);

            var receivedMessageContext = await messageContextSource.Task;
            var receivedErrorContext = await errorContextSource.Task;

            // technically they don't need to be the same instance as long as they have a shared parent
            // but because SetOnRoot is not public at the moment, this is the simplest way to verify that values can be shared across the pipelines.
            Assert.AreSame(receivedMessageContext.Extensions, receivedErrorContext.Extensions);
        }
    }
}