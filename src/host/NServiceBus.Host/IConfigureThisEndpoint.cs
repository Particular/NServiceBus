namespace NServiceBus.Host
{
    public interface IConfigureThisEndpoint
    {
        /// <summary>
        /// Used to define specific implementation details like which transport to use, serialization mechanism, subscription storage, etc.
        /// </summary>
        void Init(Configure configure);
    }
}
