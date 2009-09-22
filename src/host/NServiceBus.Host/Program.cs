using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NServiceBus.Host.Internal;
using Topshelf;
using Topshelf.Configuration;
using System.Configuration;

namespace NServiceBus.Host
{
    /// <summary>
    /// Entry point to the process.
    /// </summary>
    public class Program
    {
        private static void Main(string[] args)
        {
            Type endpointConfigurationType = GetEndpointConfigurationType();

            AssertThatEndpointConfigurationTypeHasDefaultConstructor(endpointConfigurationType);

            string endpointConfigurationFile = GetEndpointConfigurationFile(endpointConfigurationType);

            if (!File.Exists(endpointConfigurationFile))
            {
                throw new InvalidOperationException("No configuration file found at: " + endpointConfigurationFile);
            }

            var endpointConfiguration = Activator.CreateInstance(endpointConfigurationType);

            EndpointId = GetEndpointId(endpointConfiguration);

            AppDomain.CurrentDomain.SetupInformation.AppDomainInitializerArguments = args;

            IRunConfiguration cfg = RunnerConfigurator.New(x =>
            {
                x.SetDisplayName(EndpointId);
                x.SetServiceName(EndpointId);
                x.SetDescription("NServiceBus Message Endpoint Host Service");

                x.ConfigureServiceInIsolation<GenericHost>(endpointConfigurationType.AssemblyQualifiedName, c =>
                {
                    c.ConfigurationFile(endpointConfigurationFile);
                    c.CommandLineArguments(args, () => SetHostServiceLocatorArgs);
                    c.WhenStarted(service => service.Start());
                    c.WhenStopped(service => service.Stop());
                    c.CreateServiceLocator(() => new HostServiceLocator());
                });

                if (!(endpointConfiguration is ISpecify.ToStartAutomatically))
                {
                    x.DoNotStartAutomatically();
                }

                if (endpointConfiguration is ISpecify.ToRunAsLocalSystem)
                {
                    x.RunAsLocalSystem();
                }
                else
                {
                    x.RunAsFromInteractive();
                }
            });

            Runner.Host(cfg, args);
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
            string endpointName = GetEndpointName(endpointConfiguration);
            return string.Format("{0}_v{1}", endpointName, endpointConfiguration.GetType().Assembly.GetName().Version);
        }

        private static Type GetEndpointConfigurationType()
        {
            string endpoint = ConfigurationManager.AppSettings["EndpointConfigurationType"];
            if (endpoint != null)
                return Type.GetType(endpoint, true);

            IEnumerable<Type> endpoints = ScanAssembliesForEndpoints();

            ValidateEndpoints(endpoints);

            return endpoints.First();
        }

        private static IEnumerable<Type> ScanAssembliesForEndpoints()
        {
            foreach (var assembly in AssemblyScanner.GetScannableAssemblies())
                foreach (Type type in assembly.GetTypes().Where(t => typeof(IConfigureThisEndpoint).IsAssignableFrom(t) && t != typeof(IConfigureThisEndpoint)))
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
                    " Please enable Trace in NServiceBus.Host.exe.config to debug loader exceptions, " +
                    "Scanned path: " + AppDomain.CurrentDomain.BaseDirectory);
            }

            if (endpointConfigurationTypes.Count() > 1)
            {
                throw new InvalidOperationException("Host doesn't support hosting of multiple endpoints");
            }
        }

        private static string GetEndpointName(object endpointConfiguration)
        {
            string endpointName = null;

            var iHaveEndpointName = endpointConfiguration as ISpecify.EndpointName;
            if (iHaveEndpointName != null)
            {
                endpointName = iHaveEndpointName.EndpointName;
            }

            if (!string.IsNullOrEmpty(endpointName))
            {
                return endpointName;
            }

            return endpointConfiguration.GetType().FullName;
        }
    }
}