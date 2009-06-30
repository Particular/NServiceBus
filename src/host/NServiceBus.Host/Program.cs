using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NServiceBus.Host.Internal;
using Topshelf;
using Topshelf.Configuration;

namespace NServiceBus.Host
{
    public class Program
    {
        private static void Main(string[] args)
        {
            Type endpointConfigurationType = GetEndpointConfigurationType();

            string endpointId = GetEndpointId(endpointConfigurationType);

            string endpointConfigurationFile = endpointConfigurationType.Assembly.ManifestModule.Name + ".config";

            if (!File.Exists(endpointConfigurationFile))
            {
                throw new InvalidOperationException("No configuration file found at: " + endpointConfigurationFile);
            }

            IRunConfiguration cfg = RunnerConfigurator.New(x =>
            {
                x.SetDisplayName(endpointId);
                x.SetServiceName(endpointId);
                x.SetDescription("NServiceBus Message Endpoint Host Service");

                x.ConfigureServiceInIsolation<GenericHost>(endpointConfigurationType.AssemblyQualifiedName, c =>
                {
                    c.ConfigurationFile(endpointConfigurationFile);
                    c.WhenStarted(service => service.Start());
                    c.WhenStopped(service => service.Stop());
                    c.CreateServiceLocator(() => new HostServiceLocator());
                });
                x.DoNotStartAutomatically();

                x.RunAsFromInteractive();
            });

            Runner.Host(cfg, args);
        }

        public static string GetEndpointId(Type endpointConfigurationType)
        {
            string endpointName = GetEndpointName(endpointConfigurationType);
            return string.Format("{0}_v{1}", endpointName, endpointConfigurationType.Assembly.GetName().Version);
        }

        private static Type GetEndpointConfigurationType()
        {
            IEnumerable<Type> endpoints = ScanAssembliesForEndpoints();

            ValidateEndpoints(endpoints);

            return endpoints.First();
        }

        public static IEnumerable<Type> ScanAssembliesForEndpoints()
        {
            foreach (string assemblyFile in Directory.GetFiles(".", "*.dll"))
            {
                Assembly assembly = Assembly.LoadFile(Path.GetFullPath(assemblyFile));

                foreach (Type type in assembly.GetTypes().Where(t => typeof(IConfigureThisEndpoint).IsAssignableFrom(t) && t != typeof(IConfigureThisEndpoint)))
                {
                    yield return type;
                }
            }
        }

        private static void ValidateEndpoints(IEnumerable<Type> endpointConfigurationTypes)
        {
            if (endpointConfigurationTypes.Count() == 0)
            {
                throw new InvalidOperationException("No endpoints found in scanned assemlies");
            }

            if (endpointConfigurationTypes.Count() > 1)
            {
                throw new InvalidOperationException("Host doesn't support hosting of multiple endpoints");
            }
        }

        private static string GetEndpointName(Type endpointConfigurationType)
        {
            string endpointName = null;

            if (endpointConfigurationType is ISpecify.EndpointName)
            {
                endpointName = (endpointConfigurationType as ISpecify.EndpointName).EndpointName;
            }

            if (!string.IsNullOrEmpty(endpointName))
            {
                return endpointName;
            }

            return endpointConfigurationType.FullName;
        }
    }
}