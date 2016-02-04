namespace NServiceBus.Settings
{
    /// <summary>
    /// Placeholder for the various settings related to how a endpoint is scaled out.
    /// </summary>
    public partial class ScaleOutSettings
    {
        EndpointConfiguration config;

        internal ScaleOutSettings(EndpointConfiguration config)
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
