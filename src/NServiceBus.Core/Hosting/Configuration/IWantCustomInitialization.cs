namespace NServiceBus
{
    /// <summary>
    /// If you want to specify your own container or serializer,
    /// implement this interface on the class which implements <see cref="IConfigureThisEndpoint"/>.
    /// 
    /// Implementors will be invoked before the endpoint starts up.
    /// Dependency injection is not provided for these types.
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "INeedInitialization and IConfigureThisEndpoint")]
    public interface IWantCustomInitialization
    {
        /// <summary>
        /// Perform initialization logic.
        /// </summary>
        Configure Init();
    }
}
