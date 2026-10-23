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

        // The pool clears and reuses only dictionaries it created, so the headers must come from a pool.
        var pool = new HeaderPool(maxPoolSize: 1);
        var headers = pool.Rent();
        headers["HeaderPooling.Key1"] = "value1";
        headers["HeaderPooling.Key2"] = "value2";

        await SendMessage(InputQueueName, headers);

        // Keep the headers SendMessage added, such as the test id, so the pump still accepts a message
        // that a non-compliant transport builds from the reused dictionary.
        var infrastructureHeaders = new List<KeyValuePair<string, string>>();
        foreach (var header in headers)
        {
            if (!header.Key.StartsWith("HeaderPooling."))
            {
                infrastructureHeaders.Add(header);
            }
        }

        // Emulates ImmediateDispatchTerminator returning the headers once Dispatch completes, followed by
        // the next outgoing message renting the same instance and writing its own values.
        pool.Return(headers);
        var reused = pool.Rent();
        Assert.That(reused, Is.SameAs(headers), "The pool must hand out the returned dictionary again.");

        foreach (var (key, value) in infrastructureHeaders)
        {
            reused[key] = value;
        }
        reused["HeaderPooling.Key1"] = "reused-by-another-message";

        var messageContext = await messageProcessed.Task;

        Assert.That(messageContext.Headers, Is.SupersetOf(new Dictionary<string, string>
        {
            { "HeaderPooling.Key1", "value1" },
            { "HeaderPooling.Key2", "value2" },
        }), "Transports must not use the header dictionary after Dispatch completes; the framework returns it to the pool at that point.");
    }
}
