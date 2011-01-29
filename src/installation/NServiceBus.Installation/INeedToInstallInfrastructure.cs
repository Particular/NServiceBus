using System.Security.Principal;

namespace NServiceBus.Installation
{
    /// <summary>
    /// Interface invoked by the infrastructure when going to install an endpoint.
    /// Implementors invoked before <see cref="INeedToInstallSomething"/>.
    /// Implementors should not implement this type directly but rather the generic version of it.
    /// </summary>
    public interface INeedToInstallInfrastructure
    {
        /// <summary>
        /// Performs the infrastructure installation providing permission for the given user.
        /// </summary>
        /// <param name="identity">The user for whom permissions will be given.</param>
        void Install(WindowsIdentity identity);
    }

    /// <summary>
    /// Interface invoked by the infrastructure when going to install an endpoint for a specific environment.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface INeedToInstallInfrastructure<T> where T : IEnvironment
    {
        
    }
}
