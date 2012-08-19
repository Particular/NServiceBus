using System;
using System.Linq;
using System.Collections.Generic;
using NServiceBus.Unicast.Queuing.Msmq;
using NServiceBus.Hosting.Windows.Arguments;
using Topshelf.Internal;

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
        public static void Install(IEnumerable<string> args, string configFile)
        {
            // Create the new appdomain with the new config.
            var installDomain = AppDomain.CreateDomain("installDomain", AppDomain.CurrentDomain.Evidence, new AppDomainSetup
                                                                                                              {
                                                                                                                  ConfigurationFile = configFile,
                                                                                                                  AppDomainInitializer = DomainInitializer,
                                                                                                                  AppDomainInitializerArguments = args.ToArray()
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
            Parser.Args commandLineArguments = Parser.ParseArgs(args);
            var arguments = new HostArguments(commandLineArguments);

            string endpointName = string.Empty;
            if (arguments.EndpointName != null)
                endpointName = arguments.EndpointName.Value;

            string[] scannedAssemblies = null;
            if (arguments.ScannedAssemblies != null)
                scannedAssemblies = arguments.ScannedAssemblies.Value.Split(';').ToArray();
            
            if (arguments.Username != null)
            {
                MsmqUtilities.AccountToBeAssignedQueuePermissions(arguments.Username.Value);
            }
            
            host = new WindowsHost(Type.GetType(arguments.EndpointConfigurationType.Value, true), args, endpointName, commandLineArguments.Install, (arguments.InstallInfrastructure != null), scannedAssemblies);
        }

        static WindowsHost host;

    }
}