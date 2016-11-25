namespace NServiceBus.AcceptanceTests.Distributor
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using Config.ConfigurationSource;
    using Features;
    using NServiceBus.Routing.Legacy;
    using ObjectBuilder;
    using Pipeline;
    using Transport;

    public class DistributorEndpointTemplate : IEndpointSetupTemplate
    {
        public class DistributorContext : ScenarioContext
        {
            public bool ReceivedReadyMessage { get; set; }
            public string WorkerSessionId { get; set; }

            public bool IsWorkerRegistered => WorkerSessionId != null;
        }

        public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, IConfigurationSource configSource, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var config = await new DefaultServer(new List<Type>
            {
                typeof(FakeReadyMessageProcessor)
            }).GetConfiguration(runDescriptor, endpointConfiguration, configSource, configurationBuilderCustomization);

            config.EnableFeature<TimeoutManager>();
            config.EnableFeature<FakeReadyMessageProcessor>();
            return config;
        }

        class FakeReadyMessageProcessor : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                context.AddSatelliteReceiver(
                    "Ready Messages satellite",
                    context.Settings.EndpointName() + ".Control",
                    TransportTransactionMode.TransactionScope,
                    PushRuntimeSettings.Default,
                    OnError,
                    OnMessage);
                context.Pipeline.Register(new AddDistributorHeaderBehavior(), "adds the distributorworker session id header to outgoing messages");
            }

            static Task OnMessage(IBuilder builder, MessageContext message)
            {
                var testContext = builder.Build<ScenarioContext>() as DistributorContext;
                if (testContext == null)
                {
                    return Task.CompletedTask;
                }

                string sessionId;
                if (!message.Headers.TryGetValue("NServiceBus.Distributor.WorkerSessionId", out sessionId))
                {
                    return Task.CompletedTask;
                }

                testContext.WorkerSessionId = sessionId;
                string capacityString;
                if (message.Headers.TryGetValue("NServiceBus.Distributor.WorkerCapacityAvailable", out capacityString))
                {
                    var cap = int.Parse(capacityString);
                    if (cap == 1) //If it is not 1 then we got the initial ready message.
                    {
                        testContext.ReceivedReadyMessage = true;
                    }
                }
                return Task.CompletedTask;
            }

            static RecoverabilityAction OnError(RecoverabilityConfig arg1, ErrorContext arg2)
            {
                return RecoverabilityAction.ImmediateRetry();
            }

            class AddDistributorHeaderBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
            {
                public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
                {
                    var testContext = context.Builder.Build<ScenarioContext>() as DistributorContext;
                    // use the session of a registered worker if available or a random ID otherwise
                    context.Headers.Add(
                        "NServiceBus.Distributor.WorkerSessionId",
                        testContext?.WorkerSessionId ?? Guid.NewGuid().ToString());
                    return next(context);
                }
            }
        }
    }

    public static class DistributorConfigurationExtensions
    {
        public static void EnlistWithDistributor(this EndpointConfiguration config, Type distributorEndpoint)
        {
            var distributorAddress = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(distributorEndpoint);
            config.EnlistWithLegacyMSMQDistributor(distributorAddress, distributorAddress + ".Control", 10);
        }
    }
}