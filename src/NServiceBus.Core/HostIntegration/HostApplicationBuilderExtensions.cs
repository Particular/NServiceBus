#nullable enable

namespace NServiceBus;

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Configuration.AdvancedExtensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Transport;

/// <summary>
/// Extension methods to register NServiceBus endpoints with the host application builder.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Registers an NServiceBus endpoint with the specified name.
    /// </summary>
    public static void AddNServiceBusEndpoint(
        this IHostApplicationBuilder builder,
        string endpointName,
        Action<EndpointConfiguration> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointName);
        ArgumentNullException.ThrowIfNull(configure);

        var endpointKey = $"NServiceBus.Endpoint.{endpointName}";
        if (builder.Properties.ContainsKey(endpointKey))
        {
            throw new InvalidOperationException(
                $"An endpoint with the name '{endpointName}' has already been registered.");
        }
        builder.Properties[endpointKey] = true;

        var endpointConfiguration = new EndpointConfiguration(endpointName);

        configure(endpointConfiguration);

        var scanningDisabled = endpointConfiguration.AssemblyScanner().Disable;
        var scanningKey = $"NServiceBus.Scanning.{endpointName}";
        builder.Properties[scanningKey] = scanningDisabled;

        var endpointCount = builder.Properties.Keys
            .Count(k => k is string s && s.StartsWith("NServiceBus.Endpoint."));

        if (endpointCount > 1)
        {
            var endpointsWithScanning = builder.Properties
                .Where(kvp => kvp.Key is string s && s.StartsWith("NServiceBus.Scanning.") && kvp.Value is false)
                .Select(kvp => ((string)kvp.Key)["NServiceBus.Scanning.".Length..])
                .ToList();

            if (endpointsWithScanning.Count > 0)
            {
                throw new InvalidOperationException(
                    $"When multiple endpoints are registered, each endpoint must disable assembly scanning " +
                    $"(cfg.AssemblyScanner().Disable = true) and explicitly register its handlers using AddHandler<T>(). " +
                    $"The following endpoints have assembly scanning enabled: {string.Join(", ", endpointsWithScanning.Select(n => $"'{n}'"))}.");
            }
        }

        var transport = endpointConfiguration.GetSettings().Get<TransportDefinition>();
        var transportKey = $"NServiceBus.Transport.{RuntimeHelpers.GetHashCode(transport)}";
        if (builder.Properties.TryGetValue(transportKey, out var existingEndpoint))
        {
            throw new InvalidOperationException(
                $"This transport instance is already used by endpoint '{existingEndpoint}'. Each endpoint requires its own transport instance.");
        }
        builder.Properties[transportKey] = endpointName;

        var keyedServices = new KeyedServiceCollectionAdapter(builder.Services, endpointName);
        var startableEndpoint = EndpointWithExternallyManagedContainer.Create(
            endpointConfiguration, keyedServices);

        builder.Services.AddKeyedSingleton(endpointName, (sp, _) =>
            new EndpointStarter(startableEndpoint, sp, endpointName, keyedServices));

        builder.Services.AddSingleton<IHostedService, NServiceBusHostedService>(sp =>
            new NServiceBusHostedService(sp.GetRequiredKeyedService<EndpointStarter>(endpointName)));

        builder.Services.AddKeyedSingleton<IMessageSession>(endpointName, (sp, key) =>
            new HostAwareMessageSession(sp.GetRequiredKeyedService<EndpointStarter>(key)));
    }
}