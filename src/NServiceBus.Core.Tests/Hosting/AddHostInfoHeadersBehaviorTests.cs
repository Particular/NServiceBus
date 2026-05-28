namespace NServiceBus.Core.Tests.Hosting;

using System;
using System.Threading.Tasks;
using NServiceBus.Hosting;
using NUnit.Framework;
using Support;
using Testing;

[TestFixture]
public class AddHostInfoHeadersBehaviorTests
{
    [Test]
    public async Task Should_set_originating_host_headers()
    {
        var hostId = Guid.Parse("9dfac3aa-f466-4358-b349-c9858f19d5b9");
        var behavior = new AddHostInfoHeadersBehavior(new HostInformation(hostId, "display-name"), "endpoint-name");
        var context = new TestableOutgoingLogicalMessageContext();

        await behavior.Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers[Headers.OriginatingMachine], Is.EqualTo(RuntimeEnvironment.MachineName));
            Assert.That(context.Headers[Headers.OriginatingEndpoint], Is.EqualTo("endpoint-name"));
            Assert.That(context.Headers[Headers.OriginatingHostId], Is.EqualTo("9dfac3aaf4664358b349c9858f19d5b9"));
        }
    }

    [Test]
    public async Task Should_format_originating_host_id_using_n_format()
    {
        var hostId = Guid.Parse("9dfac3aa-f466-4358-b349-c9858f19d5b9");
        var behavior = new AddHostInfoHeadersBehavior(new HostInformation(hostId, "display-name"), "endpoint-name");
        var context = new TestableOutgoingLogicalMessageContext();

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.OriginatingHostId], Is.EqualTo("9dfac3aaf4664358b349c9858f19d5b9"));
    }

    [Test]
    public async Task Should_overwrite_existing_originating_host_headers()
    {
        var hostId = Guid.Parse("9dfac3aa-f466-4358-b349-c9858f19d5b9");
        var behavior = new AddHostInfoHeadersBehavior(new HostInformation(hostId, "display-name"), "endpoint-name");
        var context = new TestableOutgoingLogicalMessageContext
        {
            Headers =
            {
                [Headers.OriginatingMachine] = "existing-machine",
                [Headers.OriginatingEndpoint] = "existing-endpoint",
                [Headers.OriginatingHostId] = "existing-host-id"
            }
        };

        await behavior.Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers[Headers.OriginatingMachine], Is.EqualTo(RuntimeEnvironment.MachineName));
            Assert.That(context.Headers[Headers.OriginatingEndpoint], Is.EqualTo("endpoint-name"));
            Assert.That(context.Headers[Headers.OriginatingHostId], Is.EqualTo("9dfac3aaf4664358b349c9858f19d5b9"));
        }
    }
}
