namespace NServiceBus.Hosting.Windows.Installers
{
    using System;
    using Arguments;

    /// <summary>
    /// Windows Installer for NService Bus Host
    /// </summary>
    class WindowsInstaller
    {

        /// <summary>
        /// Run installers (infrastructure and per endpoint) and handles profiles.
        /// </summary>
        public static void Install(string[] args, string configFile)
        {
            // Create the new appDomain with the new config.
            var appDomainSetup = new AppDomainSetup
            {
                ConfigurationFile = configFile,
                AppDomainInitializer = DomainInitializer,
                AppDomainInitializerArguments = args
            };
            var installDomain = AppDomain.CreateDomain("installDomain", AppDomain.CurrentDomain.Evidence, appDomainSetup);

            // Call the right config method in that appDomain.
            var del = new CrossAppDomainDelegate(RunInstall);
            installDomain.DoCallBack(del);
        }

        static void DomainInitializer(string[] args)
        {
            Console.WriteLine("Initializing the installer in the Install AppDomain");
            var arguments = new HostArguments(args);
            string endpointName = null;

            if (arguments.EndpointName != null)
            {
                endpointName = arguments.EndpointName;
            }

            username = arguments.Username;

            var endpointType = Type.GetType(arguments.EndpointConfigurationType, true);
            host = new InstallWindowsHost(endpointType, args, endpointName, arguments.ScannedAssemblies);
        }

        /// <summary>
        /// Run Install
        /// </summary>
        static void RunInstall()
        {
            Console.WriteLine("Executing the NServiceBus installers");

            try
            {
                host.Install(username);
            }
            catch (Exception ex)
            {
                //need to suppress here in order to avoid infinite loop
                Console.WriteLine("Failed to execute installers: " +ex);  
            }
        }

        static string username;
        static InstallWindowsHost host;

    }
}