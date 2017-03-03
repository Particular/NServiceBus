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

// Disable obsolete warning until MessageEndpointMappings has been removed from config and we can remove the parameter completetely
#pragma warning disable CS0612, CS0619, CS0618
        public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, IConfigurationSource configSource, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var settings = runDescriptor.Settings;

            var types = endpointConfiguration.GetTypesScopedByTestClass();

            typesToInclude.AddRange(types);

            var configuration = new EndpointConfiguration(endpointConfiguration.EndpointName);

            configuration.TypesToIncludeInScan(typesToInclude);
            configuration.CustomConfigurationSource(configSource);
            configuration.EnableInstallers();

            configuration.DisableFeature<TimeoutManager>();

            var recoverability = configuration.Recoverability();
            recoverability.Delayed(delayed => delayed.NumberOfRetries(0));
            recoverability.Immediate(immediate => immediate.NumberOfRetries(0));
            configuration.SendFailedMessagesTo("error");

            await configuration.DefineTransport(settings, endpointConfiguration).ConfigureAwait(false);

            configuration.DefineBuilder(settings);
            configuration.RegisterComponentsAndInheritanceHierarchy(runDescriptor);

            await configuration.DefinePersistence(settings, endpointConfiguration).ConfigureAwait(false);

            configuration.GetSettings().SetDefault("ScaleOut.UseSingleBrokerQueue", true);
            configurationBuilderCustomization(configuration);

            return configuration;
        }
#pragma warning restore CS0612, CS0619, CS0618

        List<Type> typesToInclude;
    }
}