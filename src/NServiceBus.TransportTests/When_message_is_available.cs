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
            var onCompleteCalled = new TaskCompletionSource<CompleteContext>();

            OnTestTimeout(() => onCompleteCalled.SetCanceled());

            MessageContext messageContext = null;

            await StartPump((context, _) =>
            {
                messageContext = context;

                return Task.CompletedTask;
            },
            (context, _) => Task.FromResult(ErrorHandleResult.Handled),
            transactionMode,
            onComplete: (context, _) =>
            {
                onCompleteCalled.SetResult(context);
                return Task.CompletedTask;
            });

            await SendMessage(InputQueueName, new Dictionary<string, string>
            {
                {"MyHeader", "MyValue"}
            });

            var completeContext = await onCompleteCalled.Task;

            Assert.False(completeContext.OnMessageFailed, "Message failure should not be indicated");
            Assert.NotNull(messageContext, "On message should have been called");
            Assert.False(string.IsNullOrEmpty(messageContext.NativeMessageId), "Should pass the native message id");
            Assert.AreEqual("MyValue", messageContext.Headers["MyHeader"], "Should pass the message headers");
            Assert.AreEqual("", Encoding.UTF8.GetString(messageContext.Body), "Should pass the body");
        }
    }
}