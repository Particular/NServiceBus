namespace NServiceBus.Config
{
    using System.Diagnostics;

    /// <summary>
    /// Responsible for running the installers if necessary
    /// </summary>
    public class WindowsInstallerRunner : IWantToRunWhenConfigurationIsComplete
    {
        static WindowsInstallerRunner()
        {
            RunInstallers = Debugger.IsAttached;
        }

        /// <summary>
        /// True if installers should be invoked
        /// </summary>
        public static bool RunInstallers { get; set; }

        public static string RunAs { get; set; }

        /// <summary>
        /// Runs the installers if necessary.
        /// </summary>
        public void Run()
        {
            if (!RunInstallers)
            {
                return;
            }

            if (RunInstallers)
            {
                Installer<Installation.Environments.Windows>.RunOtherInstallers = true;
            }

            Configure.Instance.ForInstallationOn<Installation.Environments.Windows>(RunAs).Install();
        }
    }
}