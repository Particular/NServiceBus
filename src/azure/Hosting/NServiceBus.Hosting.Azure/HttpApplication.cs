using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using NServiceBus.Hosting.Helpers;
using NServiceBus.Integration.Azure;

namespace NServiceBus.Hosting.Azure
{
    public class HttpApplication : System.Web.HttpApplication
    {
        private static readonly Lazy<GenericHost> StartGenericHost = new Lazy<GenericHost>(StartHost);

        private const string ProfileSetting = "NServiceBus.Profile";
        private GenericHost genericHost;

        private static GenericHost StartHost()
        {
            Configure.WithWeb();

            var azureSettings = new AzureConfigurationSettings();
            var requestedProfiles = azureSettings.GetSetting(ProfileSetting);

            var endpointConfigurationType = GetEndpointConfigurationType(azureSettings);

            AssertThatEndpointConfigurationTypeHasDefaultConstructor(endpointConfigurationType);

            var specifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointConfigurationType);

            var genericHost = new GenericHost(specifier, requestedProfiles.Split(' '), null);           

            genericHost.Start();

            return genericHost;
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

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            genericHost = StartGenericHost.Value;
        }

    }
}
