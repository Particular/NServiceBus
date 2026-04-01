namespace NServiceBus;

using Installation;
using Microsoft.Extensions.DependencyInjection;

static class InstallerExternallyManaged
{
    internal static InstallerWithExternallyManagedContainer Create(EndpointConfiguration configuration,
        IServiceCollection serviceCollection)
    {
        // does not overwrite installer usernames configured by the user.
        configuration.EnableInstallers();

        var endpointCreator = EndpointCreator.Create(configuration, serviceCollection);

        return new InstallerWithExternallyManagedContainer(endpointCreator);
    }
}