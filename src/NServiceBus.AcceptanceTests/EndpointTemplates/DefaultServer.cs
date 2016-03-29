namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using Configuration.AdvanceExtensibility;
    using Features;
    using NServiceBus.Config.ConfigurationSource;
    using Serialization;

    public class DefaultServer : IEndpointSetupTemplate
    {
        public DefaultServer()
        {
            typesToInclude = new List<Type>();
        }

        public DefaultServer(List<Type> typesToInclude)
        {
            this.typesToInclude = typesToInclude;
        }

        public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, IConfigurationSource configSource, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var settings = runDescriptor.Settings;

            var types = endpointConfiguration.GetTypesScopedByTestClass();

            typesToInclude.AddRange(types);

            var builder = new EndpointConfiguration(endpointConfiguration.EndpointName);

            builder.TypesToIncludeInScan(typesToInclude);
            builder.CustomConfigurationSource(configSource);
            builder.EnableInstallers();

            builder.DisableFeature<TimeoutManager>();
            builder.DisableFeature<SecondLevelRetries>();
            builder.DisableFeature<FirstLevelRetries>();

            await builder.DefineTransport(settings, endpointConfiguration.EndpointName).ConfigureAwait(false);

            builder.DefineBuilder(settings);
            builder.RegisterComponentsAndInheritanceHierarchy(runDescriptor);

            Type serializerType;
            if (settings.TryGet("Serializer", out serializerType))
            {
                builder.UseSerialization((SerializationDefinition) Activator.CreateInstance(serializerType));
            }
            await builder.DefinePersistence(settings, endpointConfiguration.EndpointName).ConfigureAwait(false);

            builder.GetSettings().SetDefault("ScaleOut.UseSingleBrokerQueue", true);
            configurationBuilderCustomization(builder);

            return builder;
        }

        List<Type> typesToInclude;
    }
}