using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NServiceBus.Hosting.Helpers;
using NServiceBus.Hosting.Windows.Arguments;
using Topshelf;
using Topshelf.Configuration;
using System.Configuration;
using Topshelf.Internal;

namespace NServiceBus.Hosting.Windows
{
    using System.Security.Principal;
    using Installers;

    /// <summary>
    /// Entry point to the process.
    /// </summary>
    public class Program
    {
        private static AssemblyScannerResults assemblyScannerResults;
        static void Main(string[] args)
        {
            Parser.Args commandLineArguments = Parser.ParseArgs(args);
            var arguments = new HostArguments(commandLineArguments);

            if (arguments.Help != null)
            {
                DisplayHelpContent();

                return;
            }
            assemblyScannerResults = AssemblyScanner.GetScannableAssemblies();
            var endpointConfigurationType = GetEndpointConfigurationType(arguments);

            if (endpointConfigurationType == null)
            {
                if (arguments.InstallInfrastructure == null)
                    throw new InvalidOperationException("No endpoint configuration found in scanned assemblies. " +
                        "This usually happens when NServiceBus fails to load your assembly containing IConfigureThisEndpoint." +
                        " Try specifying the type explicitly in the NServiceBus.Host.exe.config using the appsetting key: EndpointConfigurationType, " +
                        "Scanned path: " + AppDomain.CurrentDomain.BaseDirectory);
                
                Console.WriteLine("Running infrastructure installers and exiting (ignoring other command line parameters if exist).");
                InstallInfrastructure();
                return;
            }

            AssertThatEndpointConfigurationTypeHasDefaultConstructor(endpointConfigurationType);
            string endpointConfigurationFile = GetEndpointConfigurationFile(endpointConfigurationType);
            string endpointName, serviceName;
            var endpointVersion = FileVersionRetriever.GetFileVersion(endpointConfigurationType);

            GetEndpointName(endpointConfigurationType, arguments, out endpointName, out serviceName);

            var displayName = serviceName + "-" + endpointVersion;

            if (arguments.SideBySide != null)
            {
                serviceName += "-" + endpointVersion;

                displayName += " (SideBySide)";
            }

            //Add the endpoint name so that the new appdomain can get it
            if (arguments.EndpointName == null && !String.IsNullOrEmpty(endpointName))
                args = args.Concat(new[] { "/endpointName:" + endpointName }).ToArray();

            //Add the ScannedAssemblies name so that the new appdomain can get it
            if (arguments.ScannedAssemblies == null)
                args = args.Concat(new[] { "/scannedassemblies:" + string.Join(";", assemblyScannerResults.Assemblies.Select(s => s.ToString()).ToArray()) }).ToArray();

            //Add the endpointConfigurationType name so that the new appdomain can get it
            if (arguments.EndpointConfigurationType == null)
                args = args.Concat(new[] { "/endpointConfigurationType:" + endpointConfigurationType.AssemblyQualifiedName }).ToArray();
            
            if ((commandLineArguments.Install) || (arguments.InstallInfrastructure != null))
                WindowsInstaller.Install(args, endpointConfigurationFile);

            if (arguments.InstallInfrastructure != null)
                return;

            IRunConfiguration cfg = RunnerConfigurator.New(x =>
                                                               {
                                                                   x.ConfigureServiceInIsolation<WindowsHost>(endpointConfigurationType.AssemblyQualifiedName, c =>
                                                                                                                                                                   {
                                                                                                                                                                       c.ConfigurationFile(endpointConfigurationFile);
                                                                                                                                                                       c.CommandLineArguments(args, () => SetHostServiceLocatorArgs);
                                                                                                                                                                       c.WhenStarted(service => service.Start());
                                                                                                                                                                       c.WhenStopped(service => service.Stop());
                                                                                                                                                                       c.CreateServiceLocator(() => new HostServiceLocator());
                                                                                                                                                                   });

                                                                   if (arguments.Username != null && arguments.Password != null)
                                                                   {
                                                                       x.RunAs(arguments.Username.Value, arguments.Password.Value);
                                                                   }
                                                                   else
                                                                   {
                                                                       x.RunAsLocalSystem();
                                                                   }

                                                                   if (arguments.StartManually != null)
                                                                   {
                                                                       x.DoNotStartAutomatically();
                                                                   }

                                                                   x.SetDisplayName(arguments.DisplayName != null ? arguments.DisplayName.Value : displayName);
                                                                   x.SetServiceName(serviceName);
                                                                   x.SetDescription(arguments.Description != null ? arguments.Description.Value : "NServiceBus Message Endpoint Host Service for " + displayName);

                                                                   var serviceCommandLine = commandLineArguments.CustomArguments.AsCommandLine();
                                                                   
                                                                   serviceCommandLine += " /serviceName:\"" + serviceName + "\"";
                                                                   
                                                                   if (!String.IsNullOrEmpty(endpointName))
                                                                   {
                                                                       serviceCommandLine += " /endpointName:\"" + endpointName + "\"";
                                                                   }

                                                                   x.SetServiceCommandLine(serviceCommandLine);

                                                                   if (arguments.DependsOn == null)
                                                                       x.DependencyOnMsmq();
                                                                   else
                                                                       foreach (var dependency in arguments.DependsOn.Value.Split(','))
                                                                           x.DependsOn(dependency);
                                                               });

            Runner.Host(cfg, args);
        }

        static void InstallInfrastructure()
        {
            Configure.With(AllAssemblies.Except("NServiceBus.Host32.exe"));

            var installer = new Installer<Installation.Environments.Windows>(WindowsIdentity.GetCurrent());
            installer.InstallInfrastructureInstallers();
        }

        static void DisplayHelpContent()
        {
            try
            {
                var stream = Assembly.GetCallingAssembly().GetManifestResourceStream("NServiceBus.Hosting.Windows.Content.Help.txt");

                if (stream != null)
                {
                    var helpText = new StreamReader(stream).ReadToEnd();

                    Console.WriteLine(helpText);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void SetHostServiceLocatorArgs(string[] args)
        {
            HostServiceLocator.Args = args;
        }

        static void AssertThatEndpointConfigurationTypeHasDefaultConstructor(Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);

            if (constructor == null)
                throw new InvalidOperationException("Endpoint configuration type needs to have a default constructor: " + type.FullName);
        }

        static string GetEndpointConfigurationFile(Type endpointConfigurationType)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, endpointConfigurationType.Assembly.ManifestModule.Name + ".config");
        }


        private static void GetEndpointName(Type endpointConfigurationType, HostArguments arguments,
                                            out string endpointName, out string serviceName)
        {
            var endpointConfiguration = Activator.CreateInstance(endpointConfigurationType);

            endpointName = null;
            serviceName = endpointConfiguration.GetType().Namespace ?? endpointConfiguration.GetType().Assembly.GetName().Name;

            if (arguments.ServiceName != null)
            {
                serviceName = arguments.ServiceName.Value;
            }

            var arr = endpointConfiguration.GetType().GetCustomAttributes(typeof (EndpointNameAttribute), false);
            if (arr.Length == 1)
            {
                endpointName = ((EndpointNameAttribute) arr[0]).Name;
                return;
            }

            var nameThisEndpoint = endpointConfiguration as INameThisEndpoint;
            if (nameThisEndpoint != null)
            {
                endpointName = nameThisEndpoint.GetName();
                return;
            }

            if (arguments.EndpointName != null)
            {
                endpointName = arguments.EndpointName.Value;
            }
        }

        static Type GetEndpointConfigurationType(HostArguments arguments)
        {
            if (arguments.EndpointConfigurationType != null)
            {
                string t = arguments.EndpointConfigurationType.Value;
                if (t != null)
                {
                    Type endpointType = Type.GetType(t, false);
                    if (endpointType == null)
                        throw new ConfigurationErrorsException(string.Format("Command line argument 'endpointConfigurationType' has specified to use the type '{0}' but that type could not be loaded.", t));

                    return endpointType;
                }
            }

            string endpoint = ConfigurationManager.AppSettings["EndpointConfigurationType"];
            if (endpoint != null)
            {
                var endpointType = Type.GetType(endpoint, false);
                if (endpointType == null)
                    throw new ConfigurationErrorsException(string.Format("The 'EndpointConfigurationType' entry in the NServiceBus.Host.exe.config has specified to use the type '{0}' but that type could not be loaded.", endpoint));

                return endpointType;
            }

            List<Type> endpoints = ScanAssembliesForEndpoints().ToList();
            if (!endpoints.Any())
            {
                Console.Out.WriteLine(assemblyScannerResults);
                return null;
            }

            AssertThatNotMoreThanOneEndpointIsDefined(endpoints);
            return endpoints.First();
        }

        static IEnumerable<Type> ScanAssembliesForEndpoints()
        {
            var scannableAssemblies = assemblyScannerResults.Assemblies;

            return scannableAssemblies.SelectMany(assembly => assembly.GetTypes().Where(
                t => typeof(IConfigureThisEndpoint).IsAssignableFrom(t)
                     && t != typeof(IConfigureThisEndpoint)
                     && !t.IsAbstract));
        }


        static void AssertThatNotMoreThanOneEndpointIsDefined(List<Type> endpointConfigurationTypes)
        {
            if (endpointConfigurationTypes.Count > 1)
            {
                throw new InvalidOperationException("Host doesn't support hosting of multiple endpoints. " +
                                                    "Endpoint classes found: " +
                                                    string.Join(", ",
                                                                endpointConfigurationTypes.Select(
                                                                    e => e.AssemblyQualifiedName).ToArray()) +
                                                    " You may have some old assemblies in your runtime directory." +
                                                    " Try right-clicking your VS project, and selecting 'Clean'."
                    );

            }
        }

    }
}
