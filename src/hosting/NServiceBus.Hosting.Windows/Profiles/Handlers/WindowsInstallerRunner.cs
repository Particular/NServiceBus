namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Config;

    /// <summary>
    /// Responsible for running the installers if necessary
    /// </summary>
    public class WindowsInstallerRunner : IWantToRunWhenConfigurationIsComplete
    {
        /// <summary>
        /// True if installers should be invoked
        /// </summary>
        public static bool RunInstallers { get; set; }
        /// <summary>
        /// True if infrastructure installers should be invoked
        /// </summary>
        public static bool RunInfrastructureInstallers { get; set; }
        /// <summary>
        /// Runs the installers if necessary.
        /// </summary>
        public void Run()
        {
            if (RunInstallers)
            {
                // if RunInfrastructureInstallers was set to true, don't override it (it must be the /install that set it).
                if((!Installer<Installation.Environments.Windows>.RunInfrastructureInstallers) && (RunInfrastructureInstallers))
                    Installer<Installation.Environments.Windows>.RunInfrastructureInstallers = RunInfrastructureInstallers;
                Configure.Instance.ForInstallationOn<Installation.Environments.Windows>().Install();
            }
        }
    }
}