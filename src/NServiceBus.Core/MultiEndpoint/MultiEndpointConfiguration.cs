#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using Configuration.AdvancedExtensibility;
using Settings;

/// <summary>
/// Configuration builder for hosting multiple endpoints within the same service collection.
/// </summary>
public sealed class MultiEndpointConfiguration
{
    /// <summary>
    /// Adds a new endpoint to the multi-endpoint host and returns its configuration object for further customization.
    /// </summary>
    /// <param name="endpointName">The name of the endpoint.</param>
    /// <param name="configure">Optional delegate used to customize the endpoint configuration.</param>
    public EndpointConfiguration AddEndpoint(string endpointName, Action<EndpointConfiguration>? configure = null) =>
        AddEndpoint(endpointName, endpointName, configure);

    /// <summary>
    /// Adds a new endpoint associated with the provided service key.
    /// </summary>
    /// <param name="serviceKey">The service key used to register endpoint specific services in the service collection.</param>
    /// <param name="endpointName">The name of the endpoint.</param>
    /// <param name="configure">Optional delegate used to customize the endpoint configuration.</param>
    public EndpointConfiguration AddEndpoint(string serviceKey, string endpointName, Action<EndpointConfiguration>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(serviceKey);
        ArgumentNullException.ThrowIfNull(endpointName);

        var configuration = new EndpointConfiguration(endpointName);
        configure?.Invoke(configuration);
        AddEndpointInternal(configuration, serviceKey);
        return configuration;
    }

    /// <summary>
    /// Adds an existing <see cref="EndpointConfiguration"/> to the multi-endpoint host.
    /// </summary>
    /// <param name="configuration">The endpoint configuration to add.</param>
    /// <param name="serviceKey">Optional service key used to register endpoint specific services. If not provided the endpoint name is used.</param>
    public void AddEndpoint(EndpointConfiguration configuration, string? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var endpointName = configuration.Settings.EndpointName();
        var resolvedKey = serviceKey ?? endpointName;
        AddEndpointInternal(configuration, resolvedKey);
    }

    internal IReadOnlyCollection<EndpointDefinition> Build() => endpointDefinitions.AsReadOnly();

    void AddEndpointInternal(EndpointConfiguration configuration, string serviceKey)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(serviceKey);

        var endpointName = configuration.Settings.EndpointName();

        if (!endpointNames.Add(endpointName))
        {
            throw new InvalidOperationException($"An endpoint named '{endpointName}' has already been added.");
        }

        if (!serviceKeys.Add(serviceKey))
        {
            throw new InvalidOperationException("Each endpoint requires a unique service key.");
        }

        endpointDefinitions.Add(new EndpointDefinition(endpointName, serviceKey, configuration));
    }

    readonly List<EndpointDefinition> endpointDefinitions = [];
    readonly HashSet<string> endpointNames = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> serviceKeys = [];

    internal readonly record struct EndpointDefinition(string EndpointName, string ServiceKey, EndpointConfiguration Configuration);
}
