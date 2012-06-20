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
    using System.Diagnostics;
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

            var endpointName = GetEndpointName(endpointConfigurationType, arguments);
            var endpointVersion = GetEndpointVersion(endpointConfigurationType);

            var serviceName = endpointName;

            if (arguments.ServiceName != null)
                serviceName = arguments.ServiceName.Value;

            var displayName = serviceName + "-" + endpointVersion;

            if (arguments.SideBySide != null)
            {
                serviceName += "-" + endpointVersion;

                displayName += " (SideBySide)";
            }

            //Add the endpoint name so that the new appdomain can get it
            if (arguments.EndpointName == null)
                args = args.Concat(new[] { "/endpointName:" + endpointName }).ToArray();

            //Add the ScannedAssemblies name so that the new appdomain can get it
            if (arguments.ScannedAssemblies == null)
                args = args.Concat(new[] { "/scannedassemblies:" + string.Join(";", assemblyScannerResults.Assemblies.Select(s => s.ToString()).ToArray()) }).ToArray();

            //Add the endpointConfigurationType name so that the new appdomain can get it
            if (arguments.EndpointConfigurationType == null)
                args = args.Concat(new[] { "/endpointConfigurationType:" + endpointConfigurationType.AssemblyQualifiedName }).ToArray();
            
            AppDomain.CurrentDomain.SetupInformation.AppDomainInitializerArguments = args;
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
                                                                   serviceCommandLine += " /endpointName:\"" + endpointName + "\"";

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

        static string GetEndpointVersion(Type endpointConfigurationType)
        {
            var fileVersion = FileVersionInfo.GetVersionInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, endpointConfigurationType.Assembly.ManifestModule.Name));

            //build a semver compliant version
            return string.Format("{0}.{1}.{2}", fileVersion.FileMajorPart, fileVersion.FileMinorPart, fileVersion.FileBuildPart);
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

        /// <summary>
        /// Gives a string which serves to identify the endpoint.
        /// </summary>
        /// <param name="endpointConfigurationType"></param>
        /// <param name="arguments"> </param>
        /// <returns></returns>
        static string GetEndpointName(Type endpointConfigurationType, HostArguments arguments)
        {
            var endpointConfiguration = Activator.CreateInstance(endpointConfigurationType);
            var endpointName = endpointConfiguration.GetType().Namespace;
            
            if (arguments.ServiceName != null)
                endpointName = arguments.ServiceName.Value;

            var arr = endpointConfiguration.GetType().GetCustomAttributes(typeof(EndpointNameAttribute), false);
            if (arr.Length == 1)
                endpointName = (arr[0] as EndpointNameAttribute).Name;

            if (endpointConfiguration is INameThisEndpoint)
                endpointName = (endpointConfiguration as INameThisEndpoint).GetName();

            if (arguments.EndpointName != null)
                endpointName = arguments.EndpointName.Value;

            return endpointName;
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

            IEnumerable<Type> endpoints = ScanAssembliesForEndpoints();
            if ((endpoints.Count() == 0))
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
            foreach (var assembly in scannableAssemblies)
                foreach (Type type in assembly.GetTypes().Where(
                        t => typeof(IConfigureThisEndpoint).IsAssignableFrom(t)
                        && t != typeof(IConfigureThisEndpoint)
                        && !t.IsAbstract))
                {
                    yield return type;
                }
        }

        
        static void AssertThatNotMoreThanOneEndpointIsDefined(IEnumerable<Type> endpointConfigurationTypes)
        {
            if (endpointConfigurationTypes.Count() > 1)
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
