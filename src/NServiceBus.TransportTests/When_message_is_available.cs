namespace NServiceBus.TransportTests;

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

        byte[] messageBody = null;

        await StartPump(
            (context, _) =>
            {
                messageBody = context.Body.ToArray();
                return onMessageInvoked.SetCompleted(context);
            },
            (_, __) => Task.FromResult(ErrorHandleResult.Handled),
            transactionMode);

        await SendMessage(InputQueueName, new Dictionary<string, string> { { "MyHeader", "MyValue" } }, body: new byte[] { 1, 2, 3 });

        var messageContext = await onMessageInvoked.Task;

        Assert.Multiple(() =>
        {
            Assert.That(string.IsNullOrEmpty(messageContext.NativeMessageId), Is.False, "Should pass the native message id");
            Assert.That(messageContext.Headers["MyHeader"], Is.EqualTo("MyValue"), "Should pass the message headers");
            Assert.That(messageBody, Is.EqualTo(new byte[] { 1, 2, 3 }), "Should pass the body");
        });
    }
}
