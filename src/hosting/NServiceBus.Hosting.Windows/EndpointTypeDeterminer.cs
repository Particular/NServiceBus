namespace NServiceBus.Hosting.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Reflection;
    using Arguments;
    using Helpers;

    /// <summary>
    ///     Determines the Endpoint Type by applying conventions.
    /// </summary>
    /// <remarks>
    ///     The first eligible Type is returned, checking (in order):
    ///     Args (for windows hosted endpoints)
    ///     Configuration
    ///     Assembly scanning for <see cref="IConfigureThisEndpoint" />
    /// </remarks>
    public class EndpointTypeDeterminer
    {
      
        /// <summary>
        ///     Initializes a new instance of the <see cref="EndpointTypeDeterminer" /> class.
        /// </summary>
        /// <param name="assemblyScannerResults">The assembly scanner results.</param>
        /// <param name="getEndpointConfigurationTypeFromConfig">A func to retrieve the endpoint configuration type from config.</param>
        /// <exception cref="System.ArgumentNullException">assemblyScannerResults</exception>
        public EndpointTypeDeterminer(AssemblyScannerResults assemblyScannerResults,
                                      Func<string> getEndpointConfigurationTypeFromConfig)
        {
            if (assemblyScannerResults == null)
            {
                throw new ArgumentNullException("assemblyScannerResults");
            }
            if (getEndpointConfigurationTypeFromConfig == null)
            {
                throw new ArgumentNullException("getEndpointConfigurationTypeFromConfig");
            }
            this.assemblyScannerResults = assemblyScannerResults;
            this.getEndpointConfigurationTypeFromConfig = getEndpointConfigurationTypeFromConfig;
        }

        internal EndpointType GetEndpointConfigurationTypeForHostedEndpoint(HostArguments arguments)
        {
            Type type;

            if (TryGetEndpointConfigurationTypeFromArguments(arguments, out type))
            {
                return new EndpointType(arguments,type);
            }

            return GetEndpointConfigurationType(arguments);
        }

        /// <summary>
        ///     Gets the type of the endpoint configuration.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">No endpoint configuration found in scanned assemblies. </exception>
        public EndpointType GetEndpointConfigurationType()
        {
            return GetEndpointConfigurationType(new HostArguments(new string[] {}));
        }
        public EndpointType GetEndpointConfigurationType(HostArguments arguments)
        {
            Type type;

            if (TryGetEndpointConfigurationTypeFromConfiguration(out type))
            {
                return new EndpointType(arguments,type);
            }

            if (TryGetEndpointConfigurationTypeFromScannedAssemblies(out type))
            {
                return new EndpointType(arguments,type);
            }

            throw new InvalidOperationException("No endpoint configuration found in scanned assemblies. " +
                                                "This usually happens when NServiceBus fails to load your assembly containing IConfigureThisEndpoint." +
                                                " Try specifying the type explicitly in the NServiceBus.Host.exe.config using the appsetting key: EndpointConfigurationType, " +
                                                "Scanned path: " + AppDomain.CurrentDomain.BaseDirectory);
        }

        bool TryGetEndpointConfigurationTypeFromArguments(HostArguments arguments, out Type type)
        {
            if (arguments.EndpointConfigurationType == null)
            {
                type = null;
                return false;
            }

            Type endpointType = Type.GetType(arguments.EndpointConfigurationType, false);
            if (endpointType == null)
            {
                throw new ConfigurationErrorsException(
                    string.Format(
                        "Command line argument 'endpointConfigurationType' has specified to use the type '{0}' but that type could not be loaded.",
                        arguments.EndpointConfigurationType));
            }
            type = endpointType;
            return true;
        }

        bool TryGetEndpointConfigurationTypeFromConfiguration(out Type type)
        {
            string endpoint = getEndpointConfigurationTypeFromConfig();
            if (endpoint == null)
            {
                type = null;
                return false;
            }

            Type endpointType = Type.GetType(endpoint, false);
            if (endpointType == null)
            {
                throw new ConfigurationErrorsException(
                    string.Format(
                        "The 'EndpointConfigurationType' entry in the NServiceBus.Host.exe.config has specified to use the type '{0}' but that type could not be loaded.",
                        endpoint));
            }

            type = endpointType;
            return true;
        }

        bool TryGetEndpointConfigurationTypeFromScannedAssemblies(out Type type)
        {
            List<Type> endpoints = ScanAssembliesForEndpoints().ToList();
            if (!endpoints.Any())
            {
                Console.Out.WriteLine(assemblyScannerResults);
                type = null;
                return false;
            }

            AssertThatNotMoreThanOneEndpointIsDefined(endpoints);
            type = endpoints.First();
            return true;
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

        IEnumerable<Type> ScanAssembliesForEndpoints()
        {
            List<Assembly> scannableAssemblies = assemblyScannerResults.Assemblies;

            return scannableAssemblies.SelectMany(assembly => assembly.GetTypes().Where(
                t => typeof (IConfigureThisEndpoint).IsAssignableFrom(t)
                     && t != typeof (IConfigureThisEndpoint)
                     && !t.IsAbstract));
        }

        readonly AssemblyScannerResults assemblyScannerResults;
        readonly Func<string> getEndpointConfigurationTypeFromConfig;
    }
}