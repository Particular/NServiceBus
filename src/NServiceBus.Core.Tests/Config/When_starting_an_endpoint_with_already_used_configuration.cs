namespace NServiceBus.Core.Tests.Config;

using NUnit.Framework;
using Host = Microsoft.Extensions.Hosting.Host;

public class When_starting_an_endpoint_with_already_used_configuration
{
    [Test]
    public void Should_throw()
    {
        var configuration = new EndpointConfiguration("Endpoint1");
        configuration.UseTransport(new LearningTransport());
        configuration.UseSerialization<SystemJsonSerializer>();
        configuration.AssemblyScanner().Disable = true;

        var firstBuilder = Host.CreateApplicationBuilder();
        firstBuilder.Services.AddNServiceBusEndpoint(configuration);

        var secondBuilder = Host.CreateApplicationBuilder();
        Assert.That(() => secondBuilder.Services.AddNServiceBusEndpoint(configuration), Throws.ArgumentException.With.Message.Contains("already used for starting an endpoint"));
    }
}