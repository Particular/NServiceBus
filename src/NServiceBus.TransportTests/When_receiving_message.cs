namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

public class When_receiving_message : NServiceBusTransportTest
{
    [TestCase(TransportTransactionMode.None)]
    [TestCase(TransportTransactionMode.ReceiveOnly)]
    [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
    [TestCase(TransportTransactionMode.TransactionScope)]
    public async Task Should_expose_receiving_address(TransportTransactionMode transactionMode)
    {
        var onError = CreateTaskCompletionSource<ErrorContext>();

        await StartPump(
            (context, _) =>
            {
                Assert.That(context.ReceiveAddress, Is.EqualTo(receiver.ReceiveAddress));
                throw new Exception("Simulated exception");
            },
            (context, _) =>
            {
                onError.SetResult(new ErrorContext(
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

        await SendMessage(InputQueueName);

        var errorContext = await onError.Task;
        Assert.That(errorContext.ReceiveAddress, Is.EqualTo(receiver.ReceiveAddress));
    }
}