#nullable enable

namespace NServiceBus.Core.Tests.Host;

using System;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NUnit.Framework;

[TestFixture]
public class ServiceCollectionExtensions_AddInstaller_Tests
{
    [Test]
    public void Should_register_single_endpoint_without_identifier()
    {
        var services = new ServiceCollection();

        Assert.DoesNotThrow(() => services.AddNServiceBusEndpointInstaller(CreateConfig("Sales")));
    }

    [Test]
    public void Should_register_single_endpoint_with_identifier()
    {
        var services = new ServiceCollection();

        Assert.DoesNotThrow(() => services.AddNServiceBusEndpointInstaller(CreateConfig("Sales"), "sales-key"));
    }

    [Test]
    public void Should_throw_when_first_endpoint_has_no_identifier_and_second_has_one()
    {
        var services = new ServiceCollection();

        services.AddNServiceBusEndpointInstaller(CreateConfig("Sales"));

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddNServiceBusEndpointInstaller(CreateConfig("Billing"), "billing-key"));

        Assert.That(ex!.Message, Does.Contain("each endpoint must provide an endpointIdentifier"));
    }

    [Test]
    public void Should_throw_when_first_endpoint_has_identifier_and_second_has_none()
    {
        var services = new ServiceCollection();

        services.AddNServiceBusEndpointInstaller(CreateConfig("Sales"), "sales-key");

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddNServiceBusEndpointInstaller(CreateConfig("Billing")));

        Assert.That(ex!.Message, Does.Contain("each endpoint must provide an endpointIdentifier"));
    }

    [Test]
    public void Should_register_multiple_endpoints_when_all_have_identifiers()
    {
        var services = new ServiceCollection();

        services.AddNServiceBusEndpointInstaller(CreateConfig("Sales"), "sales-key");

        Assert.DoesNotThrow(() => services.AddNServiceBusEndpointInstaller(CreateConfig("Billing"), "billing-key"));
    }

    [Test]
    public void Should_throw_when_multiple_endpoints_have_duplicate_identifiers()
    {
        var services = new ServiceCollection();

        services.AddNServiceBusEndpointInstaller(CreateConfig("Sales"), "shared-key");

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddNServiceBusEndpointInstaller(CreateConfig("Billing"), "shared-key"));

        Assert.That(ex!.Message, Does.Contain("An endpoint with the identifier 'shared-key' has already been registered"));
    }

    [Test]
    public void Should_throw_when_transport_not_specified()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<Exception>(() =>
        {
            var endpointConfiguration = new EndpointConfiguration("Billing");
            endpointConfiguration.UseSerialization<SystemJsonSerializer>();

            services.AddNServiceBusEndpointInstaller(endpointConfiguration);
        });

        Assert.That(ex!.Message, Does.Contain("A transport has not been configured. Use 'EndpointConfiguration.UseTransport()' to specify a transport"));
    }

    [Test]
    public void Should_throw_when_multiple_endpoints_assembly_scanning_enabled()
    {
        var services = new ServiceCollection();

        services.AddNServiceBusEndpointInstaller(CreateConfig("Sales"), "Sales");

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddNServiceBusEndpointInstaller(CreateConfig("Billing", assemblyScanningEnabled: true), "Billing"));

        Assert.That(ex!.Message, Does.Contain("When multiple endpoints are registered, each endpoint must disable assembly scanning (cfg.AssemblyScanner().Disable = true) and explicitly register its installers using the corresponding registrations methods like AddInstaller<T>(). The following endpoints have assembly scanning enabled: 'Billing'."));
    }

    [Test]
    public void Should_throw_when_used_with_add_installer()
    {
        var services = new ServiceCollection();

        services.AddNServiceBusEndpointInstaller(CreateConfig("Sales"), "Sales");

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddNServiceBusEndpoint(CreateConfig("Billing"), "Billing"));

        Assert.That(ex!.Message, Does.Contain("'AddNServiceBusEndpoint' cannot be used together with 'AddNServiceBusEndpointInstaller'."));
    }

    static EndpointConfiguration CreateConfig(string endpointName, bool assemblyScanningEnabled = false)
    {
        var config = new EndpointConfiguration(endpointName);
        config.UseSerialization<SystemJsonSerializer>();
        config.UseTransport(new LearningTransport());
        config.AssemblyScanner().Disable = !assemblyScanningEnabled;
        return config;
    }
}