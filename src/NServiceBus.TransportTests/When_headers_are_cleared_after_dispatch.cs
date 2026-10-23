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
            // Snapshot inside the invocation — never hold the raw context. Incoming headers are owned by
            // the transport, which may return them to its pool once onMessage completes.
            (context, _) => messageProcessed.SetCompleted(new MessageContext(
                context.NativeMessageId,
                new Dictionary<string, string>(context.Headers),
                context.Body.ToArray(),
                context.TransportTransaction,
                context.ReceiveAddress,
                context.Extensions)),
            (_, __) => Task.FromResult(ErrorHandleResult.Handled),
            TransportTransactionMode.None);

        // The pool only clears dictionaries it rented out, so the headers must come from it.
        var headerPool = new HeaderPool();
        var headers = headerPool.Rent();
        headers["HeaderPooling.Key1"] = "value1";
        headers["HeaderPooling.Key2"] = "value2";

        await SendMessage(InputQueueName, headers);

        // Emulates ImmediateDispatchTerminator: once the Dispatch task completes, the dispatched operation's
        // header dictionary is returned to the pool, which clears it. A compliant transport has already
        // copied or serialized the headers by then, so the received message is unaffected.
        headerPool.Return(headers);

        Assert.That(headers, Is.Empty, "Returning the headers to the pool must clear them, otherwise this test verifies nothing.");

        var messageContext = await messageProcessed.Task;

        Assert.That(messageContext.Headers, Is.SupersetOf(new Dictionary<string, string>
        {
            { "HeaderPooling.Key1", "value1" },
            { "HeaderPooling.Key2", "value2" },
        }), "Transports must consume headers during Dispatch; the framework returns the header dictionary to the pool once the Dispatch task completes.");
    }
}