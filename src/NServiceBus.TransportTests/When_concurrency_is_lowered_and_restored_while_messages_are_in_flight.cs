namespace NServiceBus.TransportTests;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

// Follows the sequence that consecutive-failure rate limiting uses: lower to 1 while messages are in flight, then restore.
public class When_concurrency_is_lowered_and_restored_while_messages_are_in_flight : NServiceBusTransportTest
{
    [TestCase(TransportTransactionMode.None)]
    [TestCase(TransportTransactionMode.ReceiveOnly)]
    [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
    [TestCase(TransportTransactionMode.TransactionScope)]
    public async Task Should_respect_each_limit(TransportTransactionMode transactionMode)
    {
        const int originalConcurrency = 4;
        const int lowerConcurrency = 1;
        const int messagesAtLowerConcurrency = 3;

        var onMessageCalls = new ConcurrentQueue<TaskCompletionSource>();
        var inFlight = 0;
        var maxInFlight = 0;
        var processed = 0;

        await StartPump(async (_, _) =>
            {
                var current = Interlocked.Increment(ref inFlight);
                InterlockedMax(ref maxInFlight, current);

                var tcs = CreateTaskCompletionSource();
                onMessageCalls.Enqueue(tcs);
                await tcs.Task;

                Interlocked.Decrement(ref inFlight);
                Interlocked.Increment(ref processed);
            },
            (errorContext, _) => throw new Exception("unexpected error", errorContext.Exception),
            transactionMode,
            pushRuntimeSettings: new PushRuntimeSettings(originalConcurrency));

        await SendMessages(originalConcurrency);
        await WaitUntil(() => onMessageCalls.Count == originalConcurrency);

        // Not awaited yet: a receiver may wait for the blocked messages before the call returns.
        var lowerConcurrencyTask = receiver.ChangeConcurrency(new PushRuntimeSettings(lowerConcurrency));
        var inFlightWhenLowered = lowerConcurrencyTask.ContinueWith(_ => Volatile.Read(ref inFlight), TaskContinuationOptions.ExecuteSynchronously);

        await Release(originalConcurrency);
        await lowerConcurrencyTask;

        Volatile.Write(ref maxInFlight, 0);
        await SendMessages(messagesAtLowerConcurrency);
        for (var i = 0; i < messagesAtLowerConcurrency; i++)
        {
            await WaitUntil(() => !onMessageCalls.IsEmpty);
            // Gives a receiver that ignores the lower limit time to start another message.
            await Task.Delay(TimeSpan.FromMilliseconds(500), TestTimeoutCancellationToken);
            await Release(1);
        }

        var maxInFlightAtLowerConcurrency = Volatile.Read(ref maxInFlight);

        await receiver.ChangeConcurrency(new PushRuntimeSettings(originalConcurrency));

        await SendMessages(originalConcurrency);
        await WaitUntil(() => onMessageCalls.Count == originalConcurrency);
        await Release(originalConcurrency);
        await WaitUntil(() => Volatile.Read(ref processed) == (2 * originalConcurrency) + messagesAtLowerConcurrency);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await inFlightWhenLowered, Is.LessThanOrEqualTo(lowerConcurrency), "more messages than the new limit were in flight when ChangeConcurrency returned");
            Assert.That(maxInFlightAtLowerConcurrency, Is.EqualTo(lowerConcurrency), "should not process more messages than the lowered limit at once");
        }

        async Task SendMessages(int count)
        {
            for (var i = 0; i < count; i++)
            {
                await SendMessage(InputQueueName);
            }
        }

        async Task Release(int count)
        {
            var released = 0;
            while (released < count)
            {
                if (onMessageCalls.TryDequeue(out var tcs))
                {
                    tcs.SetResult();
                    released++;
                    continue;
                }

                await Task.Delay(50, TestTimeoutCancellationToken);
            }
        }

        async Task WaitUntil(Func<bool> condition)
        {
            while (!condition())
            {
                await Task.Delay(50, TestTimeoutCancellationToken);
            }
        }
    }

    static void InterlockedMax(ref int location, int value)
    {
        var current = Volatile.Read(ref location);
        while (value > current)
        {
            var previous = Interlocked.CompareExchange(ref location, value, current);
            if (previous == current)
            {
                return;
            }

            current = previous;
        }
    }
}