using NServiceBus;

namespace OrderService
{
    public class DoNotStartSattelites : IWantCustomInitialization
    {
        /// <summary>
        /// Perform initialization logic.
        /// </summary>
        public void Init()
        {
            Configure.Instance
                .DisableSecondLevelRetries()
                .UseInMemoryTimeoutPersister()
                //.DisableTimeoutManager()
                ;
        }
    }

    public class UseBinarySerializer : IWantCustomInitialization
    {
        /// <summary>
        /// Perform initialization logic.
        /// </summary>
        public void Init()
        {
            Configure.Instance
                .BinarySerializer();
        }
    }
}