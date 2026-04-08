#nullable enable

namespace NServiceBus;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Includes extensions to the <see cref="HostApplicationBuilder" /> class allowing installation of configured NServiceBus endpoints.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Installs all NServiceBus endpoints registered to the <see cref="IServiceCollection" /> without starting the endpoints
    /// and without executing any user code.
    /// </summary>
    public static async Task InstallNServiceBusEndpoints(this HostApplicationBuilder hostBuilder, CancellationToken cancellationToken = default)
    {
        var endpointIdentifiers = hostBuilder.Services
            .Where(descriptor => descriptor.ServiceType == typeof(IEndpointLifecycle))
            .Select(descriptor => descriptor.ServiceKey)
            .ToArray();

        var host = hostBuilder.Build();

        var endpointLifecycles = endpointIdentifiers
            .Select(key => host.Services.GetKeyedService<IEndpointLifecycle>(key))
            .OfType<EndpointLifecycle>()
            .ToArray();

        foreach (var endpointLifecycle in endpointLifecycles)
        {
            await endpointLifecycle.Create(cancellationToken).ConfigureAwait(false);
            await endpointLifecycle.ForceRunInstallers(cancellationToken).ConfigureAwait(false);
        }
    }
}