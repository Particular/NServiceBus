namespace NServiceBus
{
    /// <summary>
    /// Implementers will be called after NServiceBus.Configure.With completes and a container
    /// has been set. 
    /// </summary>
    public interface INeedInitialization
    {
        /// <summary>
        /// Implementers will include custom initialization code here.
        /// </summary>
        void Init(Configure config);
    }
}
