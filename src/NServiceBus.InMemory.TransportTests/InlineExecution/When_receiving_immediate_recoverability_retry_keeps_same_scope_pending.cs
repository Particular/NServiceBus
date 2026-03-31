#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_receiving_immediate_recoverability_retry_keeps_same_scope_pending
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var infrastructure = await InlineExecutionTestHelper.CreateInfrastructure(broker, ["input"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];
        var firstAttemptFailed = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSecondAttempt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        object? firstScope = null;
        object? secondScope = null;
        var attempts = 0;

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            async (messageContext, cancellationToken) =>
            {
                var currentAttempt = Interlocked.Increment(ref attempts);
                var scope = InlineExecutionTestHelper.GetInlineScope(messageContext.TransportTransaction);

                if (currentAttempt == 1)
                {
                    firstScope = scope;
                    throw new InvalidOperationException("boom");
                }

                secondScope = scope;
                await releaseSecondAttempt.Task.WaitAsync(cancellationToken);
            },
            (_, _) =>
            {
                firstAttemptFailed.TrySetResult(null);
                return Task.FromResult(ErrorHandleResult.RetryRequired);
            },
            CancellationToken.None);

        await receiver.StartReceive();

        var rootTask = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input")), new TransportTransaction());

        await firstAttemptFailed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(rootTask.IsCompleted, Is.False);
            Assert.That(firstScope, Is.Not.Null);
            Assert.That(InlineExecutionTestHelper.GetPendingOperations(firstScope!), Is.EqualTo(1));
        });

        releaseSecondAttempt.TrySetResult();

        await rootTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(secondScope, Is.SameAs(firstScope));

        await receiver.StopReceive();
    }
}