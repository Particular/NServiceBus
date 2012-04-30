using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using NServiceBus.Hosting.Helpers;
using Topshelf;
using Topshelf.Configuration;
using Topshelf.Internal;

namespace NServiceBus.Hosting.Azure.HostProcess
{
    class Program
    {
        private static void Main(string[] args)
        {
            var commandLineArguments = Parser.ParseArgs(args);
            var arguments = new HostArguments(commandLineArguments);

            if (arguments.Help != null)
            {
                DisplayHelpContent();

                return;
            }

            var endpointConfigurationType = GetEndpointConfigurationType(arguments);

            AssertThatEndpointConfigurationTypeHasDefaultConstructor(endpointConfigurationType);

            var endpointConfigurationFile = GetEndpointConfigurationFile(endpointConfigurationType);

            var endpointConfiguration = Activator.CreateInstance(endpointConfigurationType);

            EndpointId = GetEndpointId(endpointConfiguration);

            AppDomain.CurrentDomain.SetupInformation.AppDomainInitializerArguments = args;

            var cfg = RunnerConfigurator.New(x =>
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

                x.SetDisplayName(arguments.DisplayName != null ? arguments.DisplayName.Value : EndpointId);
                x.SetServiceName(arguments.ServiceName != null ? arguments.ServiceName.Value : EndpointId);
                x.SetDescription(arguments.Description != null ? arguments.Description.Value : "NServiceBus Message Endpoint Host Service");
                x.DependencyOnMsmq();

                var serviceCommandLine = commandLineArguments.CustomArguments.AsCommandLine();

                if (arguments.ServiceName != null)
                {
                    serviceCommandLine += " /serviceName:\"" + arguments.ServiceName.Value + "\"";
                }

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

        private static void DisplayHelpContent()
        {
            try
            {
                var stream = Assembly.GetCallingAssembly().GetManifestResourceStream("NServiceBus.Hosting.Azure.HostProcess.Content.Help.txt");

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

        /// <summary>
        /// Gives an identifier for this endpoint
        /// </summary>
        public static string EndpointId { get; set; }

        private static void SetHostServiceLocatorArgs(string[] args)
        {
            HostServiceLocator.Args = args;
        }

        private static void AssertThatEndpointConfigurationTypeHasDefaultConstructor(Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);

            if (constructor == null)
                throw new InvalidOperationException("Endpoint configuration type needs to have a default constructor: " + type.FullName);
        }

        private static string GetEndpointConfigurationFile(Type endpointConfigurationType)
        {
            return Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                endpointConfigurationType.Assembly.ManifestModule.Name + ".config");
        }

        /// <summary>
        /// Gives a string which serves to identify the endpoint.
        /// </summary>
        /// <param name="endpointConfiguration"></param>
        /// <returns></returns>
        public static string GetEndpointId(object endpointConfiguration)
        {
            string endpointName = endpointConfiguration.GetType().FullName;
            return string.Format("{0}_v{1}", endpointName, endpointConfiguration.GetType().Assembly.GetName().Version);
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
            foreach (var assembly in AssemblyScanner.GetScannableAssemblies().Assemblies)
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
