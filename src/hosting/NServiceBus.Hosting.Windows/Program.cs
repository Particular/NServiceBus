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

    /// <summary>
    /// Entry point to the process.
    /// </summary>
    public class Program
    {
        private static void Main(string[] args)
        {
            Parser.Args commandLineArguments = Parser.ParseArgs(args);
            var arguments = new HostArguments(commandLineArguments);

            if (arguments.Help != null)
            {
                DisplayHelpContent();

                return;
            }

            Type endpointConfigurationType = GetEndpointConfigurationType(arguments);

            AssertThatEndpointConfigurationTypeHasDefaultConstructor(endpointConfigurationType);

            string endpointConfigurationFile = GetEndpointConfigurationFile(endpointConfigurationType);

          
            var endpointName = GetEndpointName(endpointConfigurationType);
            var endpointVersion = GetEndpointVersion(endpointConfigurationType);

            if (arguments.ServiceName != null)
                endpointName = arguments.ServiceName.Value;

            var serviceName = endpointName;

            var displayName = serviceName + "-" + endpointVersion;

            //add the endpoint name so that the new appdomain can get it
            args = args.Concat(new[] {endpointName}).ToArray();

            AppDomain.CurrentDomain.SetupInformation.AppDomainInitializerArguments = args;

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
                                                                   x.DependencyOnMsmq();

                                                                   var serviceCommandLine = commandLineArguments.CustomArguments.AsCommandLine();
                                                                   serviceCommandLine += " /serviceName:\"" + serviceName + "\"";

                                                                   x.SetServiceCommandLine(serviceCommandLine);

                                                                   if (arguments.DependsOn != null)
                                                                   {
                                                                       var dependencies = arguments.DependsOn.Value.Split(',');

                                                                       foreach (var dependency in dependencies)
                                                                       {
                                                                           if (dependency.ToUpper() == KnownServiceNames.Msmq)
                                                                           {
                                                                               continue;
                                                                           }

                                                                           x.DependsOn(dependency);
                                                                       }
                                                                   }
                                                               });

            Runner.Host(cfg, args);
        }

        static string GetEndpointVersion(Type endpointConfigurationType)
        {
            var fileVersion = FileVersionInfo.GetVersionInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,endpointConfigurationType.Assembly.ManifestModule.Name));
          
            //build a semver compliant version
            return string.Format("{0}.{1}.{2}",fileVersion.FileMajorPart,fileVersion.FileMinorPart,fileVersion.FileBuildPart);
        }

        private static void DisplayHelpContent()
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

        private static void SetHostServiceLocatorArgs(string[] args)
        {
            HostServiceLocator.Args = args;
            HostServiceLocator.EndpointName = args.Last();
        }

        private static void AssertThatEndpointConfigurationTypeHasDefaultConstructor(Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);

            if (constructor == null)
                throw new InvalidOperationException("Endpoint configuration type needs to have a default constructor: " + type.FullName);
        }

        private static string GetEndpointConfigurationFile(Type endpointConfigurationType)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, endpointConfigurationType.Assembly.ManifestModule.Name + ".config");
        }

        /// <summary>
        /// Gives a string which serves to identify the endpoint.
        /// </summary>
        /// <param name="endpointConfigurationType"></param>
        /// <returns></returns>
        static string GetEndpointName(Type endpointConfigurationType)
        {  
            var endpointConfiguration = Activator.CreateInstance(endpointConfigurationType);
            var endpointName = endpointConfiguration.GetType().Namespace;

            var arr = endpointConfiguration.GetType().GetCustomAttributes(typeof(EndpointNameAttribute), false);

            if (arr.Length == 1)
                endpointName = (arr[0] as EndpointNameAttribute).Name;

            if (endpointConfiguration is INameThisEndpoint)
                endpointName = (endpointConfiguration as INameThisEndpoint).GetName();
            
            return endpointName;
        }

        private static Type GetEndpointConfigurationType(HostArguments arguments)
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

            ValidateEndpoints(endpoints);

            return endpoints.First();
        }

        private static IEnumerable<Type> ScanAssembliesForEndpoints()
        {
            foreach (var assembly in AssemblyScanner.GetScannableAssemblies())
                foreach (Type type in assembly.GetTypes().Where(
                        t => typeof(IConfigureThisEndpoint).IsAssignableFrom(t)
                        && t != typeof(IConfigureThisEndpoint)
                        && !t.IsAbstract))
                {
                    yield return type;
                }
        }

        private static void ValidateEndpoints(IEnumerable<Type> endpointConfigurationTypes)
        {
            if (endpointConfigurationTypes.Count() == 0)
            {
                throw new InvalidOperationException("No endpoint configuration found in scanned assemlies. " +
                                                    "This usually happens when NServiceBus fails to load your assembly contaning IConfigureThisEndpoint." +
                                                    " Try specifying the type explicitly in the NServiceBus.Host.exe.config using the appsetting key: EndpointConfigurationType, " +
                                                    "Scanned path: " + AppDomain.CurrentDomain.BaseDirectory);
            }

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
