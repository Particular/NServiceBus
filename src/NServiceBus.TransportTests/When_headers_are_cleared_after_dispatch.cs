namespace NServiceBus.TransportTests;

using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

public class When_headers_are_cleared_after_dispatch : NServiceBusTransportTest
{
    [Test]
    public async Task Should_read_headers_synchronously_during_dispatch()
    {
        var messageProcessed = CreateTaskCompletionSource<MessageContext>();

        await StartPump(
            // Snapshot inside the invocation — never hold the raw context. Headers are owned by the
            // pipeline and are returned to the pool once Dispatch completes.
            (context, _) => messageProcessed.SetCompleted(new MessageContext(
                context.NativeMessageId,
                new Dictionary<string, string>(context.Headers),
                context.Body.ToArray(),
                context.TransportTransaction,
                context.ReceiveAddress,
                context.Extensions)),
            (_, __) => Task.FromResult(ErrorHandleResult.Handled),
            TransportTransactionMode.None);

        var headers = new Dictionary<string, string>
        {
            { "HeaderPooling.Key1", "value1" },
            { "HeaderPooling.Key2", "value2" },
        };

        await SendMessage(InputQueueName, headers);

        // Emulates ImmediateDispatchTerminator: after Dispatch completes, the dispatched operation's
        // header dictionary is returned to HeaderPool.Shared, which clears it. A compliant transport has
        // already copied/serialized the headers during Dispatch, before its first await, so the received
        // message is unaffected.
        HeaderPool.Shared.Return(headers);

        var messageContext = await messageProcessed.Task;

        Assert.That(messageContext.Headers, Is.SupersetOf(new Dictionary<string, string>
        {
            { "HeaderPooling.Key1", "value1" },
            { "HeaderPooling.Key2", "value2" },
        }), "Transports must consume headers synchronously during Dispatch; the framework returns the header dictionary to the pool once Dispatch completes.");
    }
}