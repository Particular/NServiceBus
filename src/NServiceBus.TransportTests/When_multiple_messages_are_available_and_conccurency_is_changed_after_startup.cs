namespace NServiceBus.TransportTests;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

public class When_multiple_messages_are_available_and_conccurency_is_changed_after_startup : NServiceBusTransportTest
{
    [TestCase(TransportTransactionMode.None)]
    [TestCase(TransportTransactionMode.ReceiveOnly)]
    [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
    [TestCase(TransportTransactionMode.TransactionScope)]
    public async Task A_lower_concurrency_should_make_fewer_messages_handled_concurrently(TransportTransactionMode transactionMode)
    {
        const int startConcurrencyLevel = 10;
        const int newConcurrencyLevel = 2;
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

        for (int i = 0; i < startConcurrencyLevel * 2; i++)
        {
            await SendMessage(InputQueueName);
        }

        // we need to wait because it might take a bit till the pump has invoked all pipelines
        while (onMessageCalls.Count < newConcurrencyLevel)
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

        Assert.AreEqual(newConcurrencyLevel, maximumConcurrentMessages, "should not process more messages than configured at once");
        Assert.AreEqual(startConcurrencyLevel * 2, messagesProcessed, "should process all enqueued messages");
    }

    [TestCase(TransportTransactionMode.None)]
    [TestCase(TransportTransactionMode.ReceiveOnly)]
    [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
    [TestCase(TransportTransactionMode.TransactionScope)]
    public async Task A_higher_concurrency_should_make_more_messages_handled_concurrently(TransportTransactionMode transactionMode)
    {
        const int startConcurrencyLevel = 5;
        const int newConcurrencyLevel = 10;
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

        for (int i = 0; i < startConcurrencyLevel * 2; i++)
        {
            await SendMessage(InputQueueName);
        }

        // we need to wait because it might take a bit till the pump has invoked all pipelines
        while (onMessageCalls.Count < newConcurrencyLevel)
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

        Assert.AreEqual(newConcurrencyLevel, maximumConcurrentMessages, "should not process more messages than configured at once");
        Assert.AreEqual(startConcurrencyLevel * 2, messagesProcessed, "should process all enqueued messages");
    }

    [TestCase(TransportTransactionMode.None)]
    [TestCase(TransportTransactionMode.ReceiveOnly)]
    [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
    [TestCase(TransportTransactionMode.TransactionScope)]
    public async Task Changing_concurrency_multiple_times_should_respect_the_last_value(TransportTransactionMode transactionMode)
    {
        const int startConcurrencyLevel = 5;
        const int newConcurrencyLevel = 10;
        const int lastConcurrencyLevel = 1;
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
        while (onMessageCalls.IsEmpty)
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

        Assert.AreEqual(lastConcurrencyLevel, maximumConcurrentMessages, "should not process more messages than configured at once");
        Assert.AreEqual(startConcurrencyLevel * 2, messagesProcessed, "should process all enqueued messages");
    }
}