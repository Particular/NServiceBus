namespace NServiceBus.Core.Tests.ServicePlatform.Retries;

using System.Threading.Tasks;
using NUnit.Framework;
using Testing;

[TestFixture]
public class MarkAsAcknowledgedBehaviorTests
{
    [Test]
    public async Task Should_set_acknowledgement_metadata_when_marker_state_exists()
    {
        var behavior = new MarkAsAcknowledgedBehavior();
        var context = new TestableAuditContext();
        context.Extensions.Set(MarkAsAcknowledgedBehavior.State.Instance);

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.AuditMetadata["ServiceControl.Retry.AcknowledgementSent"], Is.EqualTo(bool.TrueString.ToLowerInvariant()));
    }

    [Test]
    public async Task Should_not_set_acknowledgement_metadata_when_marker_state_is_missing()
    {
        var behavior = new MarkAsAcknowledgedBehavior();
        var context = new TestableAuditContext();

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.AuditMetadata.ContainsKey("ServiceControl.Retry.AcknowledgementSent"), Is.False);
    }
}
