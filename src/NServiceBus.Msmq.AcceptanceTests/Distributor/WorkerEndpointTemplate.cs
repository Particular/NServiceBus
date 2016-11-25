namespace NServiceBus.AcceptanceTests.Distributor
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using Config.ConfigurationSource;
    using Features;
    using Transport;

    public class WorkerEndpointTemplate : IEndpointSetupTemplate
    {
        public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, IConfigurationSource configSource, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var configuration = await new DefaultServer(new List<Type>
            {
                typeof(FakeReadyMessageProcessor)
            }).GetConfiguration(runDescriptor, endpointConfiguration, configSource, configurationBuilderCustomization);

            configuration.EnableFeature<TimeoutManager>();
            configuration.EnableFeature<FakeReadyMessageProcessor>();

            return configuration;
        }
    }

    class FakeReadyMessageProcessor : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.AddSatelliteReceiver(
                "ReadyMessages",
                "ReadyMessages",
                TransportTransactionMode.TransactionScope,
                PushRuntimeSettings.Default,
                (config, errorContext) => RecoverabilityAction.ImmediateRetry(),
                (builder, messageContext) => Task.CompletedTask);
        }
    }
}