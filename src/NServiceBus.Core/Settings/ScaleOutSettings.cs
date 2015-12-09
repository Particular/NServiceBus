namespace NServiceBus.Settings
{
    /// <summary>
    /// Placeholder for the various settings related to how a endpoint is scaled out.
    /// </summary>
    public partial class ScaleOutSettings
    {
        BusConfiguration config;

        internal ScaleOutSettings(BusConfiguration config)
        {
            this.config = config;
        }
        
        /// <summary>
        /// Makes sure that each instance of this endpoint gets a unique queue based on the user-provided discriminator.
        /// </summary>
        /// <param name="discriminator">The discriminator to use.</param>
        public void InstanceDiscriminator(string discriminator)
        {
            Guard.AgainstNullAndEmpty(nameof(discriminator), discriminator);

            config.Settings.Set("EndpointInstanceDiscriminator", discriminator);
        }
    }
}
