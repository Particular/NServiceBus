using NServiceBus;

namespace OrderService
{
    internal class SetupDataBus : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.AzureDataBus();
        }
    }
}