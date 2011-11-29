namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Config;

    /// <summary>
    /// Responsible for runnign the installers if nessesary
    /// </summary>
    public class WindowsInstallerRunner:IWantToRunWhenConfigurationIsComplete
    {
        /// <summary>
        /// True if installers should be invoked
        /// </summary>
        public static bool RunInstallers { get; set; }
        
        public void Run()
        {
            if(RunInstallers)
                Configure.Instance.ForInstallationOn<Installation.Environments.Windows>().Install();
        }
    }
}