namespace NServiceBus.Hosting.Windows.Installers
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    /// <summary>
    /// Windows Installer for NService Bus Host
    /// </summary>
    public class WindowsInstaller
    {
        /// <summary>
        /// Run installers (infrastructure and per endpoint) and handles profiles.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="endpointConfig"></param>
        /// <param name="endpointName"></param>
        /// <param name="configFile"></param>
        /// <param name="runOtherInstallers"></param>
        /// <param name="runInfrastructureInstallers"></param>
        public static void Install(IEnumerable<string> args, Type endpointConfig, string endpointName, string configFile, bool? runOtherInstallers, bool? runInfrastructureInstallers)
        {
            // Create the new appdomain with the new config.
            var installDomain = AppDomain.CreateDomain("installDomain", AppDomain.CurrentDomain.Evidence, new AppDomainSetup
                                                                                                              {
                                                                                                                  ConfigurationFile = configFile,
                                                                                                                  AppDomainInitializer = DomainInitializer,
                                                                                                                  AppDomainInitializerArguments = new[]{string.Join(";",args),endpointConfig.AssemblyQualifiedName,endpointName, runOtherInstallers.ToString(), runInfrastructureInstallers.ToString()}
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

            host = new WindowsHost( Type.GetType(args[1],true), args[0].Split(';').ToArray(), args[2], bool.Parse(args[3]), bool.Parse(args[4]));
        }

        static WindowsHost host;

    }
}