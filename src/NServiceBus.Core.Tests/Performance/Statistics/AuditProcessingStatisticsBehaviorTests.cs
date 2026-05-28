namespace NServiceBus.Core.Tests.Performance.Statistics;

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Testing;

[TestFixture]
public class AuditProcessingStatisticsBehaviorTests
{
    [Test]
    public async Task Should_write_processing_started_and_ended_metadata_when_state_exists()
    {
        var behavior = new AuditProcessingStatisticsBehavior();
        var context = new TestableAuditContext();
        var started = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        context.Extensions.Set(new ProcessingStatisticsBehavior.State { ProcessingStarted = started });

        var beforeInvoke = DateTimeOffset.UtcNow;
        await behavior.Invoke(context, _ => Task.CompletedTask);
        var afterInvoke = DateTimeOffset.UtcNow;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.AuditMetadata[Headers.ProcessingStarted], Is.EqualTo(DateTimeOffsetHelper.ToWireFormattedString(started)));

            var parsedEnded = DateTimeOffsetHelper.ToDateTimeOffset(context.AuditMetadata[Headers.ProcessingEnded]);
            Assert.That(parsedEnded, Is.GreaterThanOrEqualTo(beforeInvoke));
            Assert.That(parsedEnded, Is.LessThanOrEqualTo(afterInvoke));
        }
    }

    [Test]
    public async Task Should_call_next_and_not_write_metadata_when_state_missing()
    {
        var behavior = new AuditProcessingStatisticsBehavior();
        var context = new TestableAuditContext();
        var nextCalled = false;

        await behavior.Invoke(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(nextCalled, Is.True);
            Assert.That(context.AuditMetadata.ContainsKey(Headers.ProcessingStarted), Is.False);
            Assert.That(context.AuditMetadata.ContainsKey(Headers.ProcessingEnded), Is.False);
        }
    }
}
