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

        var scanner = configuration.AssemblyScanner();
        scanner.ExcludeAssemblies("NServiceBus.Core.Tests.dll");

        var exception = Assert.ThrowsAsync<Exception>(async () => await Endpoint.Create(configuration).ConfigureAwait(false));
        Assert.True(exception.Message.StartsWith("A serializer has not been configured"));
    }
}
