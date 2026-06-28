namespace NServiceBus.Core.Tests.Performance.Statistics;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Testing;
using Transport;

[TestFixture]
public class ProcessingStatisticsBehaviorTests
{
    [Test]
    public async Task Should_store_state_in_extensions_before_calling_next()
    {
        var behavior = new ProcessingStatisticsBehavior();
        var context = new TestableIncomingPhysicalMessageContext();
        var stateVisibleInNext = false;

        await behavior.Invoke(context, _ =>
        {
            stateVisibleInNext = context.Extensions.TryGet<ProcessingStatisticsBehavior.State>(out var _);
            return Task.CompletedTask;
        });

        Assert.That(stateVisibleInNext, Is.True);
    }

    [Test]
    public async Task Should_parse_timesent_from_header_when_present()
    {
        var behavior = new ProcessingStatisticsBehavior();
        var context = new TestableIncomingPhysicalMessageContext();
        var expectedTimeSent = new DateTimeOffset(2026, 5, 1, 12, 30, 0, TimeSpan.Zero);

        context.Message = new IncomingMessage(
            context.Message.MessageId,
            new Dictionary<string, string> { [Headers.TimeSent] = DateTimeOffsetHelper.ToWireFormattedString(expectedTimeSent) },
            Array.Empty<byte>());

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Extensions.Get<ProcessingStatisticsBehavior.State>().TimeSent, Is.EqualTo(expectedTimeSent));
    }

    [Test]
    public async Task Should_leave_timesent_null_when_header_missing()
    {
        var behavior = new ProcessingStatisticsBehavior();
        var context = new TestableIncomingPhysicalMessageContext();

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Extensions.Get<ProcessingStatisticsBehavior.State>().TimeSent, Is.Null);
    }

    [Test]
    public async Task Should_set_processing_started_before_next_and_processing_ended_after_next()
    {
        var behavior = new ProcessingStatisticsBehavior();
        var context = new TestableIncomingPhysicalMessageContext();
        DateTimeOffset startedObservedInNext = default;

        await behavior.Invoke(context, _ =>
        {
            startedObservedInNext = context.Extensions.Get<ProcessingStatisticsBehavior.State>().ProcessingStarted;
            return Task.CompletedTask;
        });

        var state = context.Extensions.Get<ProcessingStatisticsBehavior.State>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(startedObservedInNext, Is.Not.EqualTo(default(DateTimeOffset)));
            Assert.That(state.ProcessingStarted, Is.EqualTo(startedObservedInNext));
            Assert.That(state.ProcessingEnded, Is.GreaterThan(state.ProcessingStarted));
        }
    }

    [Test]
    public void Should_set_processing_ended_when_next_throws_and_preserve_exception()
    {
        var behavior = new ProcessingStatisticsBehavior();
        var context = new TestableIncomingPhysicalMessageContext();
        var expected = new Exception("next failed");

        var thrown = Assert.ThrowsAsync<Exception>(() => behavior.Invoke(context, _ => Task.FromException(expected)));

        var state = context.Extensions.Get<ProcessingStatisticsBehavior.State>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(thrown, Is.SameAs(expected));
            Assert.That(state.ProcessingEnded, Is.GreaterThan(state.ProcessingStarted));
        }
    }
}
