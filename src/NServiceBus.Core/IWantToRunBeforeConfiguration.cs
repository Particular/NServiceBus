namespace NServiceBus
{
    /// <summary>
    /// Indicates that this class contains logic that need to be executed before other configuration
    /// </summary>
    public interface IWantToRunBeforeConfiguration
    {
        /// <summary>
        /// Invoked before configuration starts
        /// </summary>
        /// <param name="configure"></param>
        void Init(Configure configure);
    }
}
