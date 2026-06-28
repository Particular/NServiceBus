namespace NServiceBus.Core.Tests.Hosting;

using System;
using System.Threading.Tasks;
using NServiceBus.Hosting;
using NUnit.Framework;
using Support;
using Testing;

[TestFixture]
public class AuditHostInformationBehaviorTests
{
    [Test]
    public async Task Should_set_audit_host_metadata()
    {
        var hostId = Guid.Parse("907ace7e-35f7-4fe3-9226-21a6d47e8f53");
        var behavior = new AuditHostInformationBehavior(new HostInformation(hostId, "display-name"), "endpoint-name");
        var context = new TestableAuditContext();

        await behavior.Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.AuditMetadata[Headers.HostId], Is.EqualTo("907ace7e35f74fe3922621a6d47e8f53"));
            Assert.That(context.AuditMetadata[Headers.HostDisplayName], Is.EqualTo("display-name"));
            Assert.That(context.AuditMetadata[Headers.ProcessingMachine], Is.EqualTo(RuntimeEnvironment.MachineName));
            Assert.That(context.AuditMetadata[Headers.ProcessingEndpoint], Is.EqualTo("endpoint-name"));
        }
    }

    [Test]
    public async Task Should_format_host_id_using_n_format()
    {
        var hostId = Guid.Parse("907ace7e-35f7-4fe3-9226-21a6d47e8f53");
        var behavior = new AuditHostInformationBehavior(new HostInformation(hostId, "display-name"), "endpoint-name");
        var context = new TestableAuditContext();

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.AuditMetadata[Headers.HostId], Is.EqualTo("907ace7e35f74fe3922621a6d47e8f53"));
    }

    [Test]
    public async Task Should_overwrite_existing_audit_host_metadata()
    {
        var hostId = Guid.Parse("907ace7e-35f7-4fe3-9226-21a6d47e8f53");
        var behavior = new AuditHostInformationBehavior(new HostInformation(hostId, "display-name"), "endpoint-name");
        var context = new TestableAuditContext();

        context.AuditMetadata[Headers.HostId] = "existing-host-id";
        context.AuditMetadata[Headers.HostDisplayName] = "existing-display-name";
        context.AuditMetadata[Headers.ProcessingMachine] = "existing-machine";
        context.AuditMetadata[Headers.ProcessingEndpoint] = "existing-endpoint";

        await behavior.Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.AuditMetadata[Headers.HostId], Is.EqualTo("907ace7e35f74fe3922621a6d47e8f53"));
            Assert.That(context.AuditMetadata[Headers.HostDisplayName], Is.EqualTo("display-name"));
            Assert.That(context.AuditMetadata[Headers.ProcessingMachine], Is.EqualTo(RuntimeEnvironment.MachineName));
            Assert.That(context.AuditMetadata[Headers.ProcessingEndpoint], Is.EqualTo("endpoint-name"));
        }
    }
}
