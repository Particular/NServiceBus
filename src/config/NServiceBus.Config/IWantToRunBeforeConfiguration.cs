namespace NServiceBus
{
    /// <summary>
    /// Indicates that this class contains logic that need to be excuted before other configuration
    /// </summary>
    public interface IWantToRunBeforeConfiguration
    {
        /// <summary>
        /// Invoked befor configuration starts
        /// </summary>
        void Init();
    }
}