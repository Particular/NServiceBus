namespace NServiceBus.TransportTests;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

public class When_multiple_messages_are_available_and_concurrency_is_increased_and_decreased_after_pump_started : NServiceBusTransportTest
{
    [TestCase(TransportTransactionMode.None)]
    [TestCase(TransportTransactionMode.ReceiveOnly)]
    [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
    [TestCase(TransportTransactionMode.TransactionScope)]
    public async Task The_last_concurrency_value_should_be_respected(TransportTransactionMode transactionMode)
    {
        const int startConcurrencyLevel = 5;
        const int newConcurrencyLevel = 10;
        const int lastConcurrencyLevel = 2;
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
            pushRuntimeSettings: new PushRuntimeSettings(startConcurrencyLevel));

        await receiver.ChangeConcurrency(new PushRuntimeSettings(newConcurrencyLevel));
        await receiver.ChangeConcurrency(new PushRuntimeSettings(lastConcurrencyLevel));

        for (int i = 0; i < startConcurrencyLevel * 2; i++)
        {
            await SendMessage(InputQueueName);
        }

        // we need to wait because it might take a bit till the pump has invoked all pipelines
        while (onMessageCalls.Count < lastConcurrencyLevel)
        {
            await Task.Delay(50, TestTimeoutCancellationToken);
        }

        int maximumConcurrentMessages = onMessageCalls.Count;

        // unblock pumps
        int messagesProcessed = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (messagesProcessed < startConcurrencyLevel * 2)
        {
            if (onMessageCalls.TryDequeue(out var messagePipelineTcs))
            {
                messagePipelineTcs.SetResult();
                messagesProcessed++;
            }
            TestTimeoutCancellationToken.ThrowIfCancellationRequested();
        }

        Assert.That(maximumConcurrentMessages, Is.EqualTo(lastConcurrencyLevel), "should not process more messages than configured at once");
        Assert.That(messagesProcessed, Is.EqualTo(startConcurrencyLevel * 2), "should process all enqueued messages");
    }
}