namespace NServiceBus.Hosting.Windows.Installers
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    /// <summary>
    /// Windos Instllaer for NService Bus Host
    /// </summary>
    public class WindowsInstaller
    {
        /// <summary>
        /// Installs 
        /// </summary>
        /// <param name="args"></param>
        /// <param name="endpointConfig"></param>
        /// <param name="endpointName"></param>
        /// <param name="configFile"></param>
   
        public static void Install(IEnumerable<string> args, Type endpointConfig, string endpointName, string configFile)
        {
            // Create the new appdomain with the new config.
            var installDomain = AppDomain.CreateDomain("installDomain", AppDomain.CurrentDomain.Evidence, new AppDomainSetup
                                                                                                              {
                                                                                                                  ConfigurationFile = configFile,
                                                                                                                  AppDomainInitializer = DomainInitializer,
                                                                                                                  AppDomainInitializerArguments = new[]{string.Join(";",args),endpointConfig.AssemblyQualifiedName,endpointName}
                                                                                                              });

            // Call the write config method in that appdomain.
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

            host = new WindowsHost( Type.GetType(args[1],true), args[0].Split(';').ToArray(), args[2]);
        }

        static WindowsHost host;
    }
}