namespace NServiceBus.Config
{
    /// <summary>
    /// Initializes the local address
    /// </summary>
    public class AddressInitializer : IWantToRunBeforeConfiguration
    {
        /// <summary>
        /// Initialize the local address
        /// </summary>
        /// <param name="configure"></param>
        public void Init(Configure configure)
        {
            if (Address.Local == null)
            {
                Address.InitializeLocalAddress(configure.GetLocalAddressName());
            }
        }
    }
}