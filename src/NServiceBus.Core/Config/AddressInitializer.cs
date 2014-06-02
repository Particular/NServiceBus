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
        public void Init(Configure configure)
        {
            if (Address.Local == null)
            {
                Address.InitializeLocalAddress(ConfigureSettingLocalAddressNameAction.GetLocalAddressName());
            }
        }
    }
}