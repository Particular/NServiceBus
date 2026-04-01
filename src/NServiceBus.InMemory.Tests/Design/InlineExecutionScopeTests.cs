namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class InlineExecutionScopeTests
{
    [Test]
    public void BeginDispatch_should_increment_pending_work()
    {
        var scope = new InlineExecutionScope(Guid.NewGuid());

        scope.BeginDispatch();

        Assert.That(scope.PendingOperations, Is.EqualTo(1));
    }

    [Test]
    public async Task Success_should_complete_only_when_pending_reaches_zero()
    {
        var scope = new InlineExecutionScope(Guid.NewGuid());
        Task completion = scope.Completion;

        scope.BeginDispatch();
        scope.BeginDispatch();

        scope.CompleteDispatchSuccess();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(completion.IsCompleted, Is.False);
            Assert.That(scope.PendingOperations, Is.EqualTo(1));
        }

        scope.CompleteDispatchSuccess();
        await completion;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(completion.IsCompletedSuccessfully, Is.True);
            Assert.That(scope.PendingOperations, Is.Zero);
        }
    }

    [Test]
    public async Task Concurrent_success_should_complete_when_last_pending_operation_finishes()
    {
        var scope = new InlineExecutionScope(Guid.NewGuid());
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        scope.BeginDispatch();
        scope.BeginDispatch();

        var first = Task.Run(async () =>
        {
            await start.Task;
            scope.CompleteDispatchSuccess();
        });

        var second = Task.Run(async () =>
        {
            await start.Task;
            scope.CompleteDispatchSuccess();
        });

        start.SetResult();

        await Task.WhenAll(first, second);
        await scope.Completion;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(scope.PendingOperations, Is.EqualTo(0));
            Assert.That(scope.Completion.IsCompletedSuccessfully, Is.True);
        }
    }

    [Test]
    public void First_terminal_failure_should_win()
    {
        var scope = new InlineExecutionScope(Guid.NewGuid());
        var first = new InvalidOperationException("first");
        var second = new ArgumentException("second");

        scope.BeginDispatch();
        scope.BeginDispatch();

        scope.CompleteDispatchFailure(first);
        scope.CompleteDispatchFailure(second);

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await scope.Completion);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception, Is.SameAs(first));
            Assert.That(scope.TerminalException, Is.SameAs(first));
        }
    }

    [Test]
    public async Task Concurrent_sibling_failures_should_not_aggregate_and_first_terminal_failure_should_win()
    {
        var scope = new InlineExecutionScope(Guid.NewGuid());
        var first = new InvalidOperationException("first");
        var second = new ArgumentException("second");
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        scope.BeginDispatch();
        scope.BeginDispatch();

        var firstFailure = Task.Run(async () =>
        {
            await start.Task;
            scope.CompleteDispatchFailure(first);
        });

        var secondFailure = Task.Run(async () =>
        {
            await start.Task;
            scope.CompleteDispatchFailure(second);
        });

        start.SetResult();

        await Task.WhenAll(firstFailure, secondFailure);

        var exception = Assert.CatchAsync(async () => await scope.Completion);

        Assert.That(exception, Is.Not.InstanceOf<AggregateException>());
        Assert.That(exception, Is.SameAs(scope.TerminalException));
        Assert.That(exception, Is.AnyOf(first, second));
    }

    [Test]
    public void Retries_should_not_decrement_pending_work()
    {
        var scope = new InlineExecutionScope(Guid.NewGuid());

        scope.BeginDispatch();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(scope.PendingOperations, Is.EqualTo(1));
            Assert.That(scope.Completion.IsCompleted, Is.False);
        }
    }

    [Test]
    public void Cancellation_should_fault_the_shared_task()
    {
        var scope = new InlineExecutionScope(Guid.NewGuid());
        var exception = new OperationCanceledException("stop");

        scope.BeginDispatch();
        scope.CompleteDispatchCanceled(exception);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(scope.Completion.IsCanceled, Is.True);
            Assert.That(scope.TerminalException, Is.SameAs(exception));
        }
    }

    [Test]
    public void Terminal_exception_should_be_nullable_until_a_failure_is_recorded()
    {
        var scope = new InlineExecutionScope(Guid.NewGuid());

        Assert.That(scope.TerminalException, Is.Null);
    }

    [Test]
    public void Completion_should_expose_a_task()
    {
        var scope = new InlineExecutionScope(Guid.NewGuid());

        Assert.That(scope.Completion, Is.InstanceOf<Task>());
    }

    [Test]
    public void Should_throw_when_marking_more_completions_than_registered_dispatches()
    {
        var scope = new InlineExecutionScope(Guid.NewGuid());

        var exception = Assert.Throws<InvalidOperationException>(() => scope.CompleteDispatchSuccess());

        Assert.That(exception!.Message, Does.Contain("pending"));
    }

    [TestCase("WithDeliveryAttempt")]
    [TestCase("WithDeliverAt")]
    public void Inline_metadata_should_be_preserved_by_envelope_cloning(string methodName)
    {
        var scope = new InlineExecutionScope(Guid.NewGuid());
        var inlineState = new InlineEnvelopeState(scope, 3, true);
        using var envelope = BrokerPayloadStore.Borrow("message-id", [1], new Dictionary<string, string>(), "destination", false, 5);
        var inlineEnvelope = envelope with { InlineState = inlineState };
        var clone = methodName switch
        {
            "WithDeliveryAttempt" => inlineEnvelope.WithDeliveryAttempt(2),
            "WithDeliverAt" => inlineEnvelope.WithDeliverAt(DateTimeOffset.UtcNow),
            _ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, null)
        };

        Assert.That(clone.InlineState, Is.SameAs(inlineState));
    }

    [TestCase("WithDeliveryAttempt")]
    [TestCase("WithDeliverAt")]
    public void Absent_inline_metadata_should_remain_absent_after_envelope_cloning(string methodName)
    {
        using var envelope = BrokerPayloadStore.Borrow("message-id", [1], new Dictionary<string, string>(), "destination", false, 5);
        var clone = methodName switch
        {
            "WithDeliveryAttempt" => envelope.WithDeliveryAttempt(2),
            "WithDeliverAt" => envelope.WithDeliverAt(DateTimeOffset.UtcNow),
            _ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, null)
        };

        Assert.That(clone.InlineState, Is.Null);
    }
}
