namespace NServiceBus.TransportTests
{
    using System.Collections.Generic;
    using System.Text;
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
            var onMessageCalled = new TaskCompletionSource<MessageContext>();

            OnTestTimeout(() => onMessageCalled.SetCanceled());

            string body = null;
            await StartPump((context, _) =>
            {
                body = Encoding.UTF8.GetString(context.Body);

                onMessageCalled.SetResult(context);
                return Task.FromResult(0);
            },
                (context, _) => Task.FromResult(ErrorHandleResult.Handled), transactionMode);

            await SendMessage(InputQueueName, new Dictionary<string, string>
            {
                {"MyHeader", "MyValue"}
            });

            var messageContext = await onMessageCalled.Task;

            Assert.False(string.IsNullOrEmpty(messageContext.NativeMessageId), "Should pass the native message id");
            Assert.AreEqual("MyValue", messageContext.Headers["MyHeader"], "Should pass the message headers");
            Assert.AreEqual("", body, "Should pass the body");
        }
    }
}