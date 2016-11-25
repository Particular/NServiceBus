namespace NServiceBus.AcceptanceTests.Distributor
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using Config.ConfigurationSource;
    using Features;

    public class WorkerEndpointTemplate : IEndpointSetupTemplate
    {
        public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, IConfigurationSource configSource, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var configuration = await new DefaultServer().GetConfiguration(runDescriptor, endpointConfiguration, configSource, configurationBuilderCustomization);

            configuration.EnableFeature<TimeoutManager>();

            return configuration;
        }
    }


}