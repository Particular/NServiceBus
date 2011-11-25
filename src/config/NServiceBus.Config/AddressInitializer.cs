namespace NServiceBus.Config
{
    /// <summary>
    /// Initializes the local address
    /// </summary>
    public class AddressInitializer:IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            if(Address.Local == null)
                Address.InitializeLocalAddress(Configure.EndpointName);
        }
    }
}