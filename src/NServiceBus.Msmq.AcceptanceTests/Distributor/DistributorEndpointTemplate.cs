namespace NServiceBus.AcceptanceTests.Distributor
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using Config.ConfigurationSource;
    using Features;

    public class DistributorEndpointTemplate : IEndpointSetupTemplate
    {
        public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, IConfigurationSource configSource, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var config = await new DefaultServer().GetConfiguration(runDescriptor, endpointConfiguration, configSource, configurationBuilderCustomization);

            config.EnableFeature<TimeoutManager>();
            config.AddHeaderToAllOutgoingMessages("NServiceBus.Distributor.WorkerSessionId", Guid.NewGuid().ToString());

            return config;
        }
    }
}