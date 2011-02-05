namespace NServiceBus.Installation
{
    /// <summary>
    /// Interface invoked by the infrastructure when going to install an endpoint.
    /// Implementors should not implement this type directly but rather the generic version of it.
    /// </summary>
    public interface INeedToInstallInfrastructure : INeedToInstallSomething
    {
        
    }

    /// <summary>
    /// Interface invoked by the infrastructure when going to install an endpoint for a specific environment.
    /// Implementors invoked before <see cref="INeedToInstallSomething"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface INeedToInstallInfrastructure<T> : INeedToInstallInfrastructure where T : IEnvironment
    {
        
    }
}
