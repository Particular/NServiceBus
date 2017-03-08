// Disable obsolete warning until MessageEndpointMappings has been removed from config and the IConfigurationSource parameter can be removed
#pragma warning disable CS0612, CS0619, CS0618
namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using Features;
    using NServiceBus.Config.ConfigurationSource;

    public class ServerWithNoDefaultPersistenceDefinitions : IEndpointSetupTemplate
    {
        public ServerWithNoDefaultPersistenceDefinitions()
        {
            typesToInclude = new List<Type>();
        }

        public ServerWithNoDefaultPersistenceDefinitions(List<Type> typesToInclude)
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
            builder.Recoverability()
                .Delayed(delayed => delayed.NumberOfRetries(0))
                .Immediate(immediate => immediate.NumberOfRetries(0));
            builder.SendFailedMessagesTo("error");

            await builder.DefineTransport(settings, endpointConfiguration).ConfigureAwait(false);

            builder.RegisterComponentsAndInheritanceHierarchy(runDescriptor);

            configurationBuilderCustomization(builder);

            return builder;
        }

        List<Type> typesToInclude;
    }
}
#pragma warning restore CS0612, CS0619, CS0618