namespace NServiceBus.Hosting.Windows.Installers
{
    using System;
    using Arguments;

    /// <summary>
    /// Windows Installer for NService Bus Host
    /// </summary>
    public class WindowsInstaller
    {
        /// <summary>
        /// Run installers (infrastructure and per endpoint) and handles profiles.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="configFile"></param>
        public static void Install(string[] args, string configFile)
        {
            // Create the new appDomain with the new config.
            var installDomain = AppDomain.CreateDomain("installDomain", AppDomain.CurrentDomain.Evidence, new AppDomainSetup
                                                                                                              {
                                                                                                                  ConfigurationFile = configFile,
                                                                                                                  AppDomainInitializer = DomainInitializer,
                                                                                                                  AppDomainInitializerArguments = args
                                                                                                              });

            // Call the right config method in that appDomain.
            var del = new CrossAppDomainDelegate(RunInstall);
            installDomain.DoCallBack(del);
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
                Configure.Instance.Builder.Dispose();
            }
            catch (Exception ex)
            {
                //need to suppress here in order to avoid infinite loop
                Console.WriteLine("Failed to execute installers: " +ex);  
            }
        }

        private static string username;

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
            
            host = new WindowsHost(Type.GetType(arguments.EndpointConfigurationType, true), args, endpointName, arguments.Install, arguments.ScannedAssemblies);
        }

        static WindowsHost host;

    }
}