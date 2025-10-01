namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

public class When_on_message_throws : NServiceBusTransportTest
{
    [TestCase(TransportTransactionMode.None)]
    [TestCase(TransportTransactionMode.ReceiveOnly)]
    [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
    [TestCase(TransportTransactionMode.TransactionScope)]
    public async Task Should_call_on_error(TransportTransactionMode transactionMode)
    {
        var onErrorCalled = CreateTaskCompletionSource<ErrorContext>();

        await StartPump(
            (_, __) => throw new Exception("Simulated exception"),
            (context, _) =>
            {
                onErrorCalled.SetResult(context);
                return Task.FromResult(ErrorHandleResult.Handled);
            },
            transactionMode);

        await SendMessage(InputQueueName, new Dictionary<string, string> { { "MyHeader", "MyValue" } });

        var errorContext = await onErrorCalled.Task;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(errorContext.Exception.Message, Is.EqualTo("Simulated exception"), "Should preserve the exception");
            Assert.That(errorContext.ImmediateProcessingFailures, Is.EqualTo(1), "Should track the number of delivery attempts");
            Assert.That(errorContext.Message.Headers["MyHeader"], Is.EqualTo("MyValue"), "Should pass the message headers");
        }
    }
}
