using Microsoft.WindowsAzure.ServiceRuntime;
using NServiceBus.Config;
using NServiceBus.Config.Conventions;
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
        private const string ProfileSetting = "AzureProfileConfig.Profiles";
        private IHost host;
        private readonly ManualResetEvent waitForStop = new ManualResetEvent(false);
        private bool doNotReturnFromRun = true;

        public RoleEntryPoint() : this(true)
        {
        }

        public RoleEntryPoint(bool doNotReturnFromRun)
        {
            this.doNotReturnFromRun = doNotReturnFromRun;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
        }
        
        public override bool OnStart()
        {
            var azureSettings = new AzureConfigurationSettings();
            var requestedProfileSetting = azureSettings.GetSetting(ProfileSetting);
            
            var endpointConfigurationType = GetEndpointConfigurationType(azureSettings);

            AssertThatEndpointConfigurationTypeHasDefaultConstructor(endpointConfigurationType);

            var specifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointConfigurationType);
            var requestedProfiles = requestedProfileSetting.Split(' ');
            requestedProfiles = AddProfilesFromConfiguration(requestedProfiles);

            //var endpointName = "Put somethingt smart here Yves"; // wonder if I live up to the expectations :)
            var endpointName = RoleEnvironment.IsAvailable ? RoleEnvironment.CurrentRoleInstance.Role.Name : GetType().Name;

            if (specifier is AsA_Host)
            {
                host = new DynamicHostController(specifier, requestedProfiles, new List<Type> { typeof(Development) }, endpointName);
            }
            else
            {
                host = new GenericHost(specifier, requestedProfiles, new List<Type> { typeof(Development), typeof(OnAzureTableStorage) }, endpointName);
            }

            return true;
        }

        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Trace.WriteLine("Unhandled exception occured: " + e.ExceptionObject.ToString());
        }

        public override void Run()
        {
            host.Start();
            if(doNotReturnFromRun) waitForStop.WaitOne();
        }

        public override void OnStop()
        {
            host.Stop();
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
            return AssemblyScanner.GetScannableAssemblies().Assemblies.SelectMany(
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

        private static string[] AddProfilesFromConfiguration(IEnumerable<string> args)
        {
            var list = new List<string>(args);

            var configSection = Configure.GetConfigSection<AzureProfileConfig>();

            if (configSection != null)
            {
                var configuredProfiles = configSection.Profiles.Split(',');
                Array.ForEach(configuredProfiles, s => list.Add(s.Trim()));
            }

            return list.ToArray();
        }
    }
}