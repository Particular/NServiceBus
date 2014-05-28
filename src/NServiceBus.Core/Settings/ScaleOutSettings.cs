namespace NServiceBus.Settings
{
    /// <summary>
    /// Placeholder for the various settings related to how a endpoint is scaled out
    /// </summary>
    public class ScaleOutSettings
    {
        readonly Configure config;

        public ScaleOutSettings(Configure config)
        {
            this.config = config;
        }

        /// <summary>
        /// Instructs the broker based transports to use a single queue for the endpoint regardless of which machine its running on. 
        /// This is suitable for backend processing endpoints and is the default for the As_aServer role.
        /// Clients that needs to make use of callbacks needs to make sure that this setting is off since they need to have a unique 
        /// input queue per machine in order to not miss any of the callbacks.
        /// </summary>
        public void UseSingleBrokerQueue()
        {
            config.Settings.Set("ScaleOut.UseSingleBrokerQueue", true);
        }

        /// <summary>
        /// Instructs the broker based transports to use a separate queue per endpoint when running on multiple machines. 
        /// This allows clients to make use of callbacks. This setting is the default.
        /// </summary>
        public void UseUniqueBrokerQueuePerMachine()
        {
            config.Settings.Set("ScaleOut.UseSingleBrokerQueue", false);
        }
    }
}
