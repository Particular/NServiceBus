using System;
using System.Linq;
using System.Collections.Generic;
using NServiceBus.Unicast.Queuing.Msmq;
using NServiceBus.Hosting.Windows.Arguments;

namespace NServiceBus.Hosting.Windows.Installers
{


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
            // Create the new appdomain with the new config.
            var installDomain = AppDomain.CreateDomain("installDomain", AppDomain.CurrentDomain.Evidence, new AppDomainSetup
                                                                                                              {
                                                                                                                  ConfigurationFile = configFile,
                                                                                                                  AppDomainInitializer = DomainInitializer,
                                                                                                                  AppDomainInitializerArguments = args
                                                                                                              });

            // Call the right config method in that appdomain.
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
                host.Install();
            }
            catch (Exception ex)
            {
                //need to suppress here in order to avoid infinite loop
                Console.WriteLine("Failed to execute installers: " +ex);  
            }
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
            
            if (arguments.Username != null)
            {
                MsmqUtilities.AccountToBeAssignedQueuePermissions(arguments.Username);
            }

            host = new WindowsHost(Type.GetType(arguments.EndpointConfigurationType, true), args, endpointName, arguments.Install, arguments.InstallInfrastructure, arguments.ScannedAssemblies);
        }

        static WindowsHost host;

    }
}