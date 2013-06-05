namespace NServiceBus.Installation
{
    /// <summary>
    /// Interface invoked by the infrastructure when going to install an endpoint.
    /// Implementors should not implement this type directly but rather the generic version of it.
    /// </summary>
    [ObsoleteEx(Message = "It is possible to use NServiceBus installer or powershell support to accomplish the same thing. Please see http://particular.net/articles/managing-nservicebus-using-powershell for more information.", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
    public interface INeedToInstallInfrastructure : INeedToInstallSomething
    {
        
    }

    /// <summary>
    /// Interface invoked by the infrastructure when going to install an endpoint for a specific environment.
    /// Implementors invoked before <see cref="INeedToInstallSomething"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [ObsoleteEx(Message = "It is possible to use NServiceBus installer or powershell support to accomplish the same thing. Please see http://particular.net/articles/managing-nservicebus-using-powershell for more information.", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
    public interface INeedToInstallInfrastructure<T> : INeedToInstallInfrastructure where T : IEnvironment
    {
        
    }
}
