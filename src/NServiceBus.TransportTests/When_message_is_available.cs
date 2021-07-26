namespace NServiceBus.TransportTests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_message_is_available : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_invoke_on_message(TransportTransactionMode transactionMode)
        {
            var onMessageInvoked = CreateTaskCompletionSource<MessageContext>();

            await StartPump(
                (context, _) => onMessageInvoked.SetCompleted(context),
                (_, __) => Task.FromResult(ErrorHandleResult.Handled),
                transactionMode);

            await SendMessage(InputQueueName, new Dictionary<string, string> { { "MyHeader", "MyValue" } }, body: new byte[] { 1, 2, 3 });

            var messageContext = await onMessageInvoked.Task;

            Assert.False(string.IsNullOrEmpty(messageContext.NativeMessageId), "Should pass the native message id");
            Assert.AreEqual("MyValue", messageContext.Headers["MyHeader"], "Should pass the message headers");
            Assert.AreEqual(new byte[] { 1, 2, 3 }, messageContext.Body.ToArray(), "Should pass the body");
        }
    }
}
