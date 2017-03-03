namespace NServiceBus.Transport.Msmq.AcceptanceTests.Distributor
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using Config.ConfigurationSource;
    using Features;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using ObjectBuilder;
    using Pipeline;
    using Routing.Legacy;
    using Transport;

    public class DistributorEndpointTemplate : IEndpointSetupTemplate
    {
        public class DistributorContext : ScenarioContext
        {
            public bool ReceivedReadyMessage { get; set; }
            public string WorkerSessionId { get; set; }

            public bool IsWorkerRegistered => WorkerSessionId != null;
        }

// Disable obsolete warning until MessageEndpointMappings has been removed from config and we can remove the parameter completetely
#pragma warning disable CS0612, CS0619, CS0618
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
#pragma warning restore CS0612, CS0619, CS0618

        class FakeReadyMessageProcessor : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                context.AddSatelliteReceiver(
                    "Ready Messages satellite",
                    context.Settings.EndpointName() + ".Control",
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
                    // use the session of a registered worker
                    var testContext = context.Builder.Build<ScenarioContext>() as DistributorContext;

                    if (testContext?.WorkerSessionId == null)
                    {
                        throw new Exception($"The distributor endpoint can only send messages after a worker has registered itself. Ensure to use the `DistributorContext.IsWorkerRegistered` check before sending messages from the distributor and enlist your worker with the this distributor endpoint using `{nameof(DistributorConfigurationExtensions.EnlistWithDistributor)}` extension.");
                    }

                    context.Headers.Add(
                        "NServiceBus.Distributor.WorkerSessionId",
                        testContext.WorkerSessionId);

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