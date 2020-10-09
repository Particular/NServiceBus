namespace NServiceBus
{
    using Pipeline;

    partial class RoutingComponent
    {
        RoutingComponent(UnicastSendRouter unicastSendRouter, bool enforceBestPractices)
        {
            EnforceBestPractices = enforceBestPractices;
            UnicastSendRouter = unicastSendRouter;
        }

        public bool EnforceBestPractices { get; }

        public UnicastSendRouter UnicastSendRouter { get; }

        public static RoutingComponent Initialize(
            Configuration configuration,
            TransportSeam transportSeam,
            ReceiveComponent.Configuration receiveConfiguration,
            Conventions conventions,
            PipelineSettings pipelineSettings)
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
                pipelineSettings.Register(
                    new ApplyReplyToAddressBehavior(
                        receiveConfiguration.LocalAddress,
                        receiveConfiguration.InstanceSpecificQueue,
                        configuration.PublicReturnAddress),
                    "Applies the public reply to address to outgoing messages");
            }

            var sendRouter = new UnicastSendRouter(
                isSendOnlyEndpoint,
                receiveConfiguration?.QueueNameBase,
                receiveConfiguration?.InstanceSpecificQueue,
                distributionPolicy,
                unicastRoutingTable,
                endpointInstances,
                i => transportSeam.TransportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i).ToEndpointAddress()));

            if (configuration.EnforceBestPractices)
            {
                EnableBestPracticeEnforcement(pipelineSettings, new Validations(conventions));
            }

            return new RoutingComponent(
                sendRouter,
                configuration.EnforceBestPractices);
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