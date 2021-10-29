namespace NServiceBus
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Transport;
    using Pipeline;

    partial class RoutingComponent
    {
        RoutingComponent(bool enforceBestPractices)
        {
            EnforceBestPractices = enforceBestPractices;
        }

        public bool EnforceBestPractices { get; }

        public UnicastSendRouter UnicastSendRouterBuilder(IServiceProvider serviceProvider) =>
            serviceProvider.GetRequiredService<UnicastSendRouter>();

        public static RoutingComponent Initialize(
            Configuration configuration,
            ReceiveComponent.Configuration receiveConfiguration,
            Conventions conventions,
            PipelineSettings pipelineSettings,
            HostingComponent.Configuration hostingConfiguration)
        {
            var distributionPolicy = configuration.DistributionPolicy;
            var unicastRoutingTable = configuration.UnicastRoutingTable;
            var endpointInstances = configuration.EndpointInstances;

            foreach (var distributionStrategy in configuration.CustomDistributionStrategies)
            {
                distributionPolicy.SetDistributionStrategy(distributionStrategy);
            }

            configuration.ConfiguredUnicastRoutes?.Apply(unicastRoutingTable, conventions);

            var isSendOnlyEndpoint = receiveConfiguration.IsSendOnlyEndpoint;
            if (!isSendOnlyEndpoint)
            {
                pipelineSettings.Register(sp =>
                        new ApplyReplyToAddressBehavior(
                        sp.GetRequiredService<ReceiveAddresses>(),
                        configuration.PublicReturnAddress),
                    "Applies the public reply to address to outgoing messages");
            }

            hostingConfiguration.Services.AddSingleton(sp =>
                new UnicastSendRouter(
                    isSendOnlyEndpoint,
                    receiveConfiguration.QueueNameBase,
                    receiveConfiguration.InstanceSpecificQueueAddress,
                    distributionPolicy,
                    unicastRoutingTable,
                    endpointInstances,
                    sp.GetRequiredService<ITransportAddressResolver>()));

            if (configuration.EnforceBestPractices)
            {
                EnableBestPracticeEnforcement(pipelineSettings, new Validations(conventions));
            }

            return new RoutingComponent(configuration.EnforceBestPractices);
        }

        static void EnableBestPracticeEnforcement(PipelineSettings pipeline, Validations validations)
        {
            pipeline.Register(
                "EnforceSendBestPractices",
                new EnforceSendBestPracticesBehavior(validations),
                "Enforces send messaging best practices");

            pipeline.Register(
                "EnforceReplyBestPractices",
                new EnforceReplyBestPracticesBehavior(validations),
                "Enforces reply messaging best practices");

            pipeline.Register(
                "EnforcePublishBestPractices",
                new EnforcePublishBestPracticesBehavior(validations),
                "Enforces publish messaging best practices");

            pipeline.Register(
                "EnforceSubscribeBestPractices",
                new EnforceSubscribeBestPracticesBehavior(validations),
                "Enforces subscribe messaging best practices");

            pipeline.Register(
                "EnforceUnsubscribeBestPractices",
                new EnforceUnsubscribeBestPracticesBehavior(validations),
                "Enforces unsubscribe messaging best practices");
        }
    }
}