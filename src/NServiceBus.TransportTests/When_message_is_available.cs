namespace NServiceBus.TransportTests;

using System;
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
            (context, _) => onMessageInvoked.SetCompleted(new MessageContext(
                context.NativeMessageId,
                new Dictionary<string, string>(context.Headers),
                context.Body.ToArray(),
                context.TransportTransaction,
                context.ReceiveAddress,
                context.Extensions)),
            (_, __) => Task.FromResult(ErrorHandleResult.Handled),
            transactionMode);

        await SendMessage(InputQueueName, new Dictionary<string, string> { { "MyHeader", "MyValue" } }, body: [1, 2, 3]);

        var messageContext = await onMessageInvoked.Task;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(string.IsNullOrEmpty(messageContext.NativeMessageId), Is.False, "Should pass the native message id");
            Assert.That(messageContext.Headers["MyHeader"], Is.EqualTo("MyValue"), "Should pass the message headers");
            Assert.That(messageContext.Body.Span.SequenceEqual(new byte[] { 1, 2, 3 }), Is.True, "Should pass the body");
        }
    }
}