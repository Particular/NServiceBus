namespace NServiceBus.Core.Tests.Transports.Learning;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Routing;
using NUnit.Framework;
using Transport;

public class LearningTransportMessagePumpTests
{
    [Test]
    public async Task Should_keep_default_concurrency_of_one_after_changing_back_to_default_settings()
    {
        const int numberOfMessages = 3;
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, $"change-concurrency-{Guid.NewGuid():N}");
        var receiveSettings = new ReceiveSettings("main", new QueueAddress(InputQueue), false, false, "error");
        var pump = new LearningTransportMessagePump("main", InputQueue, path, (_, _, _) => { }, null, receiveSettings, TransportTransactionMode.ReceiveOnly);

        var concurrentMessages = 0;
        var maxConcurrentMessages = 0;
        var processedMessages = 0;
        var allProcessed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            await pump.Initialize(PushRuntimeSettings.Default, async (_, cancellationToken) =>
            {
                var current = Interlocked.Increment(ref concurrentMessages);
                InterlockedMax(ref maxConcurrentMessages, current);

                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);

                Interlocked.Decrement(ref concurrentMessages);
                if (Interlocked.Increment(ref processedMessages) == numberOfMessages)
                {
                    allProcessed.TrySetResult();
                }
            }, (_, _) => Task.FromResult(ErrorHandleResult.Handled));

            await pump.StartReceive();
            await pump.ChangeConcurrency(new PushRuntimeSettings(5));
            await pump.ChangeConcurrency(PushRuntimeSettings.Default);

            var dispatcher = new LearningTransportDispatcher(path, 64);
            for (var i = 0; i < numberOfMessages; i++)
            {
                var message = new OutgoingMessage(Guid.NewGuid().ToString(), [], ReadOnlyMemory<byte>.Empty);
                await dispatcher.Dispatch(new TransportOperations(new TransportOperation(message, new UnicastAddressTag(InputQueue))), new TransportTransaction());
            }

            await allProcessed.Task.WaitAsync(TimeSpan.FromSeconds(30));

            Assert.That(maxConcurrentMessages, Is.EqualTo(1));
        }
        finally
        {
            await pump.StopReceive();

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
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

    const string InputQueue = "input";
}