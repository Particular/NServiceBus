namespace NServiceBus.TransportTests;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

public class When_multiple_messages_are_available : NServiceBusTransportTest
{
    [TestCase(TransportTransactionMode.None)]
    [TestCase(TransportTransactionMode.ReceiveOnly)]
    [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
    [TestCase(TransportTransactionMode.TransactionScope)]
    public async Task Should_handle_messages_concurrently(TransportTransactionMode transactionMode)
    {
        const int concurrencyLevel = 10;
        var onMessageCalls = new ConcurrentQueue<TaskCompletionSource>();

        await StartPump(async (context, _) =>
            {
                var tcs = CreateTaskCompletionSource();
                onMessageCalls.Enqueue(tcs);
                // "block" current pipeline invocation
                await tcs.Task;
            },
            (errorContext, __) => throw new Exception("unexpected error", errorContext.Exception),
            transactionMode,
            pushRuntimeSettings: new PushRuntimeSettings(concurrencyLevel));

        for (int i = 0; i < concurrencyLevel * 2; i++)
        {
            await SendMessage(InputQueueName);
        }

        // we need to wait because it might take a bit till the pump has invoked all pipelines
        while (onMessageCalls.Count < concurrencyLevel)
        {
            await Task.Delay(50, TestTimeoutCancellationToken);
        }

        int maximumConcurrentMessages = onMessageCalls.Count;

        // unblock pumps
        int messagesProcessed = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (messagesProcessed < concurrencyLevel * 2)
        {
            if (onMessageCalls.TryDequeue(out var messagePipelineTcs))
            {
                messagePipelineTcs.SetResult();
                messagesProcessed++;
            }
            TestTimeoutCancellationToken.ThrowIfCancellationRequested();
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(maximumConcurrentMessages, Is.EqualTo(concurrencyLevel), "should not process more messages than configured at once");
            Assert.That(messagesProcessed, Is.EqualTo(concurrencyLevel * 2), "should process all enqueued messages");
        }
    }
}