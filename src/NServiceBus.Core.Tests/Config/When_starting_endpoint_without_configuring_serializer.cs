namespace NServiceBus.Core.Tests.Config;

using System;
using NUnit.Framework;

[TestFixture]
class When_starting_endpoint_without_configuring_serializer
{
    [Test]
    public void Should_throw()
    {
        var configuration = new EndpointConfiguration("Endpoint1");
        configuration.UseTransport(new LearningTransport());
        configuration.AssemblyScanner().Disable = true;

#pragma warning disable CS0618 // Type or member is obsolete
        var exception = Assert.ThrowsAsync<Exception>(async () => await Endpoint.Create(configuration).ConfigureAwait(false));
#pragma warning restore CS0618 // Type or member is obsolete
        Assert.That(exception.Message, Does.StartWith("A serializer has not been configured"));
    }
}
