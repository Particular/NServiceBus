namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

public class When_modifying_headers_before_on_error : NServiceBusTransportTest
{
    [TestCase(TransportTransactionMode.None)]
    [TestCase(TransportTransactionMode.ReceiveOnly)]
    [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
    [TestCase(TransportTransactionMode.TransactionScope)]
    public async Task Should_roll_back(TransportTransactionMode transactionMode)
    {
        var errorHandled = CreateTaskCompletionSource<ErrorContext>();

        await StartPump(
            (context, _) =>
            {
                context.Headers["test-header"] = "modified";
                throw new Exception();
            },
            (context, __) =>
            {
                errorHandled.SetResult(new ErrorContext(
                    context.Exception,
                    new Dictionary<string, string>(context.Headers),
                    context.NativeMessageId,
                    context.Body.ToArray(),
                    context.TransportTransaction,
                    context.ImmediateProcessingFailures,
                    context.ReceiveAddress,
                    context.Extensions));
                return Task.FromResult(ErrorHandleResult.Handled);
            },
            transactionMode);

        await SendMessage(InputQueueName, new Dictionary<string, string> { { "test-header", "original" } });

        var errorContext = await errorHandled.Task;

        Assert.That(errorContext.Headers["test-header"], Is.EqualTo("original"));
    }
}