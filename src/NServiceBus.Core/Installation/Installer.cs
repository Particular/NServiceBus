namespace NServiceBus.Installation;

using System;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Threading;

/// <summary>
/// TODO
/// </summary>
public static class Installer
{

    /// <summary>
    /// Executes all the installers and transport configuration without starting the endpoint.
    /// <see cref="Setup"/> always runs installers, even if <see cref="InstallConfigExtensions.EnableInstallers"/> has not been configured.
    /// </summary>
    /// <param name="configuration">The endpoint configuration.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    public static async Task Setup(EndpointConfiguration configuration, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(nameof(configuration), configuration);

        // required for downstreams checking HostingConfiguration.ShouldRunInstallers.
        // does not overwrite installer usernames configured by the user.
        // will leak setting modifications but re-use of the EndpointConfiguration isn't supported at the moment.
        configuration.EnableInstallers();

        var serviceCollection = new ServiceCollection();
        var endpointCreator = EndpointCreator.Create(configuration, serviceCollection);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        await using (serviceProvider.ConfigureAwait(false))
        {
            var endpoint = endpointCreator.CreateStartableEndpoint(serviceProvider, true);
            await endpoint.RunInstallers(cancellationToken).ConfigureAwait(false);
            await endpoint.Setup(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="serviceCollection"></param>
    /// <returns></returns>
    public static InstallerWithExternallyManagedContainer CreateInstallerWithExternallyManagedContainer(EndpointConfiguration configuration, IServiceCollection serviceCollection)
    {
        Guard.AgainstNull(nameof(configuration), configuration);
        Guard.AgainstNull(nameof(serviceCollection), serviceCollection);

        // required for downstreams checking HostingConfiguration.ShouldRunInstallers.
        // does not overwrite installer usernames configured by the user.
        // will leak setting modifications but re-use of the EndpointConfiguration isn't supported at the moment.
        configuration.EnableInstallers();

        var endpointCreator = EndpointCreator.Create(configuration, serviceCollection);

        return new InstallerWithExternallyManagedContainer(endpointCreator);
    }
}

/// <summary>
/// TODO
/// </summary>
public class InstallerWithExternallyManagedContainer
{
    readonly EndpointCreator endpointCreator;

    internal InstallerWithExternallyManagedContainer(EndpointCreator endpointCreator)
    {
        this.endpointCreator = endpointCreator;
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="cancellationToken"></param>
    public async Task Run(IServiceProvider builder, CancellationToken cancellationToken = default)
    {
        var endpoint = endpointCreator.CreateStartableEndpoint(builder, false);
        await endpoint.RunInstallers(cancellationToken).ConfigureAwait(false);
        await endpoint.Setup(cancellationToken).ConfigureAwait(false);
    }
}