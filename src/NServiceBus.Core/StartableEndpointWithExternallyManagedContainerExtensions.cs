namespace NServiceBus;
using System.Threading.Tasks;
using System.Threading;
using System;

/// <summary>
/// Extensions methods for <see cref="IStartableEndpointWithExternallyManagedContainer"/>.
/// </summary>
public static class StartableEndpointWithExternallyManagedContainerExtensions
{
    /// <summary>
    /// Executes all the installers and transport configuration without starting the endpoint.
    /// <see cref="Install"/> always runs installers, even if <see cref="InstallConfigExtensions.EnableInstallers"/> has not been configured.
    /// </summary>
    public static Task Install(this IStartableEndpointWithExternallyManagedContainer startableEndpoint, IServiceProvider builder, CancellationToken cancellationToken = default) =>
        ((ExternallyManagedContainerHost)startableEndpoint).Install(builder, cancellationToken);
}