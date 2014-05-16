namespace NServiceBus.Settings
{
    /// <summary>
    /// Placeholder for the various settings related to how a endpoint is scaled out
    /// </summary>
    public class ScaleOutSettings
    {
        /// <summary>
        /// Instructs the broker based transports to use a single queue for the endpoint regardless of which machine its running on. 
        /// This is suitable for backend processing endpoints and is the default for the As_aServer role.
        /// Clients that needs to make use of callbacks needs to make sure that this setting is off since they need to have a unique 
        /// input queue per machine in order to not miss any of the callbacks.
        /// </summary>
        public ScaleOutSettings UseSingleBrokerQueue()
        {
            SettingsHolder.Instance.Set("ScaleOut.UseSingleBrokerQueue", true);

            return this;
        }

        /// <summary>
        /// Instructs the broker based transports to use a separate queue per endpoint when running on multiple machines. 
        /// This allows clients to make use of callbacks. This setting is the default.
        /// </summary>
        public ScaleOutSettings UseUniqueBrokerQueuePerMachine()
        {
            SettingsHolder.Instance.Set("ScaleOut.UseSingleBrokerQueue", false);

            return this;
        }
    }
}