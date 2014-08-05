namespace NServiceBus.Hosting.Profiles
{
    /// <summary>
    /// Activates the profiles to be used
    /// </summary>
    class ProfileActivator : IWantToRunBeforeConfigurationIsFinalized
    {
        /// <summary>
        /// The profile manager
        /// </summary>
        public static ProfileManager ProfileManager { get; set; }
        
        /// <summary>
        /// Activate profile handlers
        /// </summary>
        public void Run(Configure config)
        {
            if (ProfileManager != null)
            {
                ProfileManager.ActivateProfileHandlers(config);
            }
        }
    }
}