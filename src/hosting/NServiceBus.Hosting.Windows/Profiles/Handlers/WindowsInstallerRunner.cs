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
            if ((!RunInstallers) && (!RunInfrastructureInstallers)) 
                return;
            
            if(RunInfrastructureInstallers)
                Installer<Installation.Environments.Windows>.RunInfrastructureInstallers = RunInfrastructureInstallers;

            if (RunInstallers)
                Installer<Installation.Environments.Windows>.RunOtherInstallers = RunInstallers;

            Configure.Instance.ForInstallationOn<Installation.Environments.Windows>().Install();
        }
    }
}