namespace NServiceBus.Core.Tests;

using System;
using NUnit.Framework;

[TestFixture]
public class EndpointConfigurationTests
{
    [Theory]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public void When_creating_configuration_with_empty_endpoint_name_should_throw(string name)
    {
        var exception = Assert.Throws<ArgumentException>(() => new EndpointConfiguration(name));

        Assert.That(exception.Message, Does.Contain("Endpoint name must not be empty").And.Contain("endpointName"));
    }

    [Test]
    public void When_creating_configuration_with_invalid_character_in_endpoint_name_should_throw()
    {
        var exception = Assert.Throws<ArgumentException>(() => new EndpointConfiguration("endpoint@V6"));

        Assert.That(exception.Message, Does.Contain("Endpoint name must not contain an '@' character.").And.Contain("endpointName"));
    }

    [Test]
    public void OpenTelemetry_should_be_enabled_by_default()
    {
        var configuration = new EndpointConfiguration("TestEndpoint");

        var hostingSettings = configuration.Settings.Get<HostingComponent.Settings>();

        Assert.That(hostingSettings.EnableOpenTelemetry, Is.True);
    }

    [Test]
    public void DisableOpenTelemetry_should_disable_OpenTelemetry()
    {
        var configuration = new EndpointConfiguration("TestEndpoint");

        configuration.DisableOpenTelemetry();

        var hostingSettings = configuration.Settings.Get<HostingComponent.Settings>();
        Assert.That(hostingSettings.EnableOpenTelemetry, Is.False);
    }
}
