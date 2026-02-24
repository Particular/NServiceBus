#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Configuration.AdvancedExtensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Transport;

/// <summary>
/// Extension methods to register NServiceBus endpoints with the service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers an NServiceBus endpoint.
    /// </summary>
    public static void AddNServiceBusEndpoint(
        this IServiceCollection services,
        EndpointConfiguration endpointConfiguration,
        object? endpointIdentifier = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(endpointConfiguration);

        var endpointName = endpointConfiguration.GetSettings().EndpointName();
        var transport = endpointConfiguration.GetSettings().Get<TransportDefinition>();
        var registrations = GetExistingRegistrations(services);

        ValidateEndpointName(endpointName, registrations);
        ValidateEndpointIdentifier(endpointIdentifier, registrations);
        ValidateAssemblyScanning(endpointConfiguration, endpointName, registrations);
        ValidateTransportReuse(transport, registrations);

        if (endpointIdentifier is null)
        {
            var startableEndpoint = EndpointWithExternallyManagedContainer.Create(endpointConfiguration, services);

            services.AddSingleton<IEndpointStarter>(sp => new UnkeyedEndpointStarter(startableEndpoint, sp));
            services.AddSingleton<IHostedService, NServiceBusHostedService>(sp =>
                new NServiceBusHostedService(sp.GetRequiredService<IEndpointStarter>()));
            services.AddSingleton<IMessageSession>(sp =>
                new HostAwareMessageSession(sp.GetRequiredService<IEndpointStarter>()));
        }
        else
        {
            var keyedServices = new KeyedServiceCollectionAdapter(services, endpointIdentifier);
            var startableEndpoint = EndpointWithExternallyManagedContainer.Create(endpointConfiguration, keyedServices);

            services.AddKeyedSingleton<IEndpointStarter>(endpointIdentifier, (sp, _) =>
                new EndpointStarter(startableEndpoint, sp, endpointIdentifier, keyedServices));

            services.AddSingleton<IHostedService, NServiceBusHostedService>(sp =>
                new NServiceBusHostedService(sp.GetRequiredKeyedService<IEndpointStarter>(endpointIdentifier)));

            services.AddKeyedSingleton<IMessageSession>(endpointIdentifier, (sp, key) =>
                new HostAwareMessageSession(sp.GetRequiredKeyedService<IEndpointStarter>(key!)));
        }

        services.AddSingleton(new EndpointRegistration(endpointName, endpointIdentifier, endpointConfiguration.AssemblyScanner().Disable, RuntimeHelpers.GetHashCode(transport)));
    }

    static void ValidateEndpointName(string endpointName, List<EndpointRegistration> registrations)
    {
        if (registrations.Any(r => r.EndpointName == endpointName))
        {
            throw new InvalidOperationException(
                $"An endpoint with the name '{endpointName}' has already been registered.");
        }
    }

    static void ValidateEndpointIdentifier(object? endpointIdentifier, List<EndpointRegistration> registrations)
    {
        if (registrations.Count == 0)
        {
            return;
        }

        if (endpointIdentifier is null || registrations.Any(r => r.EndpointIdentifier is null))
        {
            throw new InvalidOperationException(
                "When multiple endpoints are registered, each endpoint must provide an endpointIdentifier.");
        }

        if (registrations.Any(r => Equals(r.EndpointIdentifier, endpointIdentifier)))
        {
            throw new InvalidOperationException(
                $"An endpoint with the identifier '{endpointIdentifier}' has already been registered.");
        }
    }

    static void ValidateAssemblyScanning(EndpointConfiguration endpointConfiguration, string endpointName, List<EndpointRegistration> registrations)
    {
        var endpoints = registrations
            .Append(new EndpointRegistration(endpointName, null, endpointConfiguration.AssemblyScanner().Disable, 0))
            .ToList();

        if (endpoints.Count <= 1)
        {
            return;
        }

        var endpointsWithScanning = endpoints
            .Where(r => !r.ScanningDisabled)
            .Select(r => r.EndpointName)
            .ToList();

        if (endpointsWithScanning.Count > 0)
        {
            throw new InvalidOperationException(
                $"When multiple endpoints are registered, each endpoint must disable assembly scanning " +
                $"(cfg.AssemblyScanner().Disable = true) and explicitly register its handlers using AddHandler<T>(). " +
                $"The following endpoints have assembly scanning enabled: {string.Join(", ", endpointsWithScanning.Select(n => $"'{n}'"))}.");
        }
    }

    static void ValidateTransportReuse(TransportDefinition transport, List<EndpointRegistration> registrations)
    {
        var transportHash = RuntimeHelpers.GetHashCode(transport);
        var existingRegistration = registrations.FirstOrDefault(r => r.TransportHashCode == transportHash);
        if (existingRegistration is not null)
        {
            throw new InvalidOperationException(
                $"This transport instance is already used by endpoint '{existingRegistration.EndpointName}'. Each endpoint requires its own transport instance.");
        }
    }

    static List<EndpointRegistration> GetExistingRegistrations(IServiceCollection services) =>
        [.. services
            .Where(d => d.ServiceType == typeof(EndpointRegistration) && d.ImplementationInstance is EndpointRegistration)
            .Select(d => (EndpointRegistration)d.ImplementationInstance!)];

    sealed record EndpointRegistration(string EndpointName, object? EndpointIdentifier, bool ScanningDisabled, int TransportHashCode);
}