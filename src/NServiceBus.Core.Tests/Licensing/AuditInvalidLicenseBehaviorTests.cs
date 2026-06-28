namespace NServiceBus.Core.Tests.Licensing;

using System.Threading.Tasks;
using NUnit.Framework;
using Testing;

[TestFixture]
public class AuditInvalidLicenseBehaviorTests
{
    [Test]
    public async Task Should_set_license_expired_metadata_before_calling_next()
    {
        var behavior = new AuditInvalidLicenseBehavior();
        var context = new TestableAuditContext();

        var metadataValueObservedInNext = string.Empty;

        await behavior.Invoke(context, _ =>
        {
            metadataValueObservedInNext = context.AuditMetadata[Headers.HasLicenseExpired];
            return Task.CompletedTask;
        });

        Assert.That(metadataValueObservedInNext, Is.EqualTo(bool.TrueString.ToLowerInvariant()));
    }
}
