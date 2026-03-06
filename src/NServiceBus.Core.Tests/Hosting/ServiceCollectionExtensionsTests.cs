#nullable enable

namespace NServiceBus.Core.Tests.Host;

using System;
using Microsoft.Extensions.DependencyInjection;
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
    public void Should_throw_when_identifier_is_set_for_single_endpoint()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddNServiceBusEndpoint(CreateConfig("Sales"), "custom-key"));

        Assert.That(ex!.Message, Does.Contain("cannot be set when registering a single endpoint"));
    }

    [Test]
    public void Should_register_multiple_endpoints_without_explicit_identifiers()
    {
        var services = new ServiceCollection();

        services.AddNServiceBusEndpoint(CreateConfig("Sales"));
        Assert.DoesNotThrow(() => services.AddNServiceBusEndpoint(CreateConfig("Billing")));
    }

    [Test]
    public void Should_register_multiple_endpoints_with_explicit_identifier_on_subsequent_endpoints()
    {
        var services = new ServiceCollection();

        services.AddNServiceBusEndpoint(CreateConfig("Sales"));
        Assert.DoesNotThrow(() => services.AddNServiceBusEndpoint(CreateConfig("Billing"), "billing-key"));
    }

    [Test]
    public void Should_throw_when_second_endpoint_has_same_name_used_as_fallback_identifier()
    {
        var services = new ServiceCollection();
        const string sharedName = "Sales";

        services.AddNServiceBusEndpoint(CreateConfig("FirstEndpoint"));
        services.AddNServiceBusEndpoint(CreateConfig(sharedName));

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddNServiceBusEndpoint(CreateConfig(sharedName)));

        Assert.That(ex!.Message, Does.Contain($"An endpoint with the identifier '{sharedName}' has already been registered"));
    }

    [Test]
    public void Should_throw_when_multiple_endpoints_have_duplicate_explicit_identifier()
    {
        var services = new ServiceCollection();

        services.AddNServiceBusEndpoint(CreateConfig("Sales"));
        services.AddNServiceBusEndpoint(CreateConfig("Billing"), "shared-key");

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddNServiceBusEndpoint(CreateConfig("Shipping"), "shared-key"));

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