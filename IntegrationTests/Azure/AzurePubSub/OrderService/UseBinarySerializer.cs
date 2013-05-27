using NServiceBus;

namespace OrderService
{
    public class UseBinarySerializer : IWantCustomInitialization
    {
        /// <summary>
        /// Perform initialization logic.
        /// </summary>
        public void Init()
        {
            Configure.Serialization.Binary();
        }
    }
}