using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using NServiceBus.Hosting.Helpers;
using NServiceBus.Integration.Azure;
using System.Threading;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

namespace NServiceBus.Hosting.Azure
{
    /// <summary>
    /// A host implementation for the Azure cloud platform
    /// </summary>
    public class RoleEntryPoint : Microsoft.WindowsAzure.ServiceRuntime.RoleEntryPoint
    {
        private const string ProfileSetting = "NServiceBus.Profile";
        private GenericHost genericHost;
        private readonly ManualResetEvent waitForStop = new ManualResetEvent(false);

        public RoleEntryPoint()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;
        }
        
        public override bool OnStart()
        {
            var azureSettings = new AzureConfigurationSettings();
            var requestedProfiles = azureSettings.GetSetting(ProfileSetting);
            
            var endpointConfigurationType = GetEndpointConfigurationType(azureSettings);

            AssertThatEndpointConfigurationTypeHasDefaultConstructor(endpointConfigurationType);

            var specifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointConfigurationType);

            genericHost = new GenericHost(specifier, requestedProfiles.Split(' '), null);           

            return true;
        }

        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Trace.WriteLine("Unhandled exception occured: " + e.ExceptionObject.ToString());
        }

        static System.Reflection.Assembly CurrentDomainAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Trace.WriteLine("Couldn't load assembly: " + args.Name);
            return null;
        }

        public override void Run()
        {
            genericHost.Start();
            waitForStop.WaitOne();
        }

        public override void OnStop()
        {
            genericHost.Stop();
            waitForStop.Set();
        }

        private static void AssertThatEndpointConfigurationTypeHasDefaultConstructor(Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);

            if (constructor == null)
                throw new InvalidOperationException("Endpoint configuration type needs to have a default constructor: " + type.FullName);
        }

        private static Type GetEndpointConfigurationType(AzureConfigurationSettings settings)
        {
            string endpoint = settings.GetSetting("EndpointConfigurationType");
            if (!String.IsNullOrEmpty(endpoint))
            {
                var endpointType = Type.GetType(endpoint, false);
                if (endpointType == null)
                    throw new ConfigurationErrorsException(string.Format("The 'EndpointConfigurationType' entry in the role config has specified to use the type '{0}' but that type could not be loaded.", endpoint));

                return endpointType;
            }

            IEnumerable<Type> endpoints = ScanAssembliesForEndpoints();

            ValidateEndpoints(endpoints);

            return endpoints.First();
        }

        private static IEnumerable<Type> ScanAssembliesForEndpoints()
        {
        	return AssemblyScanner.GetScannableAssemblies().SelectMany(
        		assembly => assembly.GetTypes().Where(
        			t => typeof(IConfigureThisEndpoint).IsAssignableFrom(t)
        			     && t != typeof(IConfigureThisEndpoint)
        			     && !t.IsAbstract));
        }

        private static void ValidateEndpoints(IEnumerable<Type> endpointConfigurationTypes)
        {
            if (endpointConfigurationTypes.Count() == 0)
            {
                throw new InvalidOperationException("No endpoint configuration found in scanned assemlies. " +
                                                    "This usually happens when NServiceBus fails to load your assembly containing IConfigureThisEndpoint." +
                                                    " Try specifying the type explicitly in the roles config using the appsetting key: EndpointConfigurationType, " +
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