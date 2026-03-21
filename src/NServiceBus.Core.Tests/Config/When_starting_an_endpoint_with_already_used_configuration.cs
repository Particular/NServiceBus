namespace NServiceBus.Core.Tests.Config;

using System;
using System.Threading.Tasks;
using NUnit.Framework;

public class When_starting_an_endpoint_with_already_used_configuration
{
    [Test]
    public async Task Should_throw()
    {
        var configuration = new EndpointConfiguration("Endpoint1");
        configuration.UseTransport(new LearningTransport());
        configuration.UseSerialization<SystemJsonSerializer>();
        configuration.AssemblyScanner().Disable = true;

#pragma warning disable CS0618 // Type or member is obsolete
        await Endpoint.Create(configuration).ConfigureAwait(false);
        Assert.ThrowsAsync<ArgumentException>(async () => await Endpoint.Create(configuration).ConfigureAwait(false));
#pragma warning restore CS0618 // Type or member is obsolete
    }
}