namespace NServiceBus
{
    /// <summary>
    /// If you want to specify your own container or serializer,
    /// implement this interface on the class which implements <see cref="IConfigureThisEndpoint"/>.
    /// 
    /// Implementors will be invoked before the endpoint starts up.
    /// Dependency injection is not provided for these types.
    /// </summary>
    public interface IWantCustomInitialization
    {
        /// <summary>
        /// Perform initialization logic.
        /// </summary>
        void Init();
    }
}
