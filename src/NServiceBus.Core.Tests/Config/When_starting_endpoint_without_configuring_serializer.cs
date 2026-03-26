namespace NServiceBus.Core.Tests.Config;

using NUnit.Framework;
using Host = Microsoft.Extensions.Hosting.Host;

[TestFixture]
class When_starting_endpoint_without_configuring_serializer
{
    [Test]
    public void Should_throw()
    {
        var configuration = new EndpointConfiguration("Endpoint1");
        configuration.UseTransport(new LearningTransport());
        configuration.AssemblyScanner().Disable = true;

        var builder = Host.CreateApplicationBuilder();

        Assert.That(() => builder.Services.AddNServiceBusEndpoint(configuration), Throws.Exception.With.Message.Contains("A serializer has not been configured"));
    }
}