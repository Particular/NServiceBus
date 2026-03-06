#nullable enable

namespace NServiceBus.Core.Tests.Host;

using System;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    [Test]
    public void Should_register_single_endpoint_without_identifier()
    {
        var services = new ServiceCollection();

        Assert.DoesNotThrow(() => services.AddNServiceBusEndpoint(CreateConfig("Sales")));
    }

    [Test]
    public void Should_register_single_endpoint_with_identifier()
    {
        var services = new ServiceCollection();

        Assert.DoesNotThrow(() => services.AddNServiceBusEndpoint(CreateConfig("Sales"), "sales-key"));
    }

    [Test]
    public void Should_throw_when_first_endpoint_has_no_identifier_and_second_has_one()
    {
        var services = new ServiceCollection();

        services.AddNServiceBusEndpoint(CreateConfig("Sales"));

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddNServiceBusEndpoint(CreateConfig("Billing"), "billing-key"));

        Assert.That(ex!.Message, Does.Contain("each endpoint must provide an endpointIdentifier"));
    }

    [Test]
    public void Should_throw_when_first_endpoint_has_identifier_and_second_has_none()
    {
        var services = new ServiceCollection();

        services.AddNServiceBusEndpoint(CreateConfig("Sales"), "sales-key");

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddNServiceBusEndpoint(CreateConfig("Billing")));

        Assert.That(ex!.Message, Does.Contain("each endpoint must provide an endpointIdentifier"));
    }

    [Test]
    public void Should_register_multiple_endpoints_when_all_have_identifiers()
    {
        var services = new ServiceCollection();

        services.AddNServiceBusEndpoint(CreateConfig("Sales"), "sales-key");

        Assert.DoesNotThrow(() => services.AddNServiceBusEndpoint(CreateConfig("Billing"), "billing-key"));
    }

    [Test]
    public void Should_throw_when_multiple_endpoints_have_duplicate_identifiers()
    {
        var services = new ServiceCollection();

        services.AddNServiceBusEndpoint(CreateConfig("Sales"), "shared-key");

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddNServiceBusEndpoint(CreateConfig("Billing"), "shared-key"));

        Assert.That(ex!.Message, Does.Contain("An endpoint with the identifier 'shared-key' has already been registered"));
    }

    static EndpointConfiguration CreateConfig(string endpointName)
    {
        var config = new EndpointConfiguration(endpointName);
        config.UseSerialization<SystemJsonSerializer>();
        config.UseTransport(new LearningTransport());
        config.AssemblyScanner().Disable = true;
        return config;
    }
}