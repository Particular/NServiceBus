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
            MessageContext messageContext = null;

            var completed = new TaskCompletionSource<CompleteContext>();
            OnTestTimeout(() => completed.SetCanceled());

            await StartPump(
                (context, _) =>
                {
                    messageContext = context;
                    return Task.CompletedTask;
                },
                (context, _) => Task.FromResult(ErrorHandleResult.Handled),
                (context, _) => completed.SetCompleted(context),
                transactionMode);

            await SendMessage(InputQueueName, new Dictionary<string, string> { { "MyHeader", "MyValue" } }, body: new byte[] { 1, 2, 3 });

            var completeContext = await completed.Task;

            Assert.False(completeContext.OnMessageFailed, "Message failure should not be indicated");
            Assert.NotNull(messageContext, "On message should have been called");
            Assert.False(string.IsNullOrEmpty(messageContext.NativeMessageId), "Should pass the native message id");
            Assert.AreEqual("MyValue", messageContext.Headers["MyHeader"], "Should pass the message headers");
            Assert.AreEqual(new byte[] { 1, 2, 3 }, messageContext.Body, "Should pass the body");
        }
    }
}
