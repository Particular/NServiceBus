namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.Config.ConfigurationSource;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    public class DefaultPublisher : IEndpointSetupTemplate
    {
        public BusConfiguration GetConfiguration(RunDescriptor runDescriptor, EndpointConfiguration endpointConfiguration, IConfigurationSource configSource, Action<BusConfiguration> configurationBuilderCustomization)
        {
            return new DefaultServer(new List<Type> { typeof(SubscriptionTracer), typeof(SubscriptionTracer.Registration) }).GetConfiguration(runDescriptor, endpointConfiguration, configSource, b =>
            {
                b.Pipeline.Register<SubscriptionTracer.Registration>();
                configurationBuilderCustomization(b);
            });
        }

        class SubscriptionTracer : IBehavior<OutgoingContext>
        {
            public ScenarioContext Context { get; set; }

            public void Invoke(OutgoingContext context, Action next)
            {
                next();

                List<Address> subscribers;

                if (context.TryGet("SubscribersForEvent", out  subscribers))
                {
                    Context.AddTrace(string.Format("Subscribers for {0} : {1}", context.OutgoingLogicalMessage.MessageType.Name, string.Join(";", subscribers)));
                }

                bool nosubscribers;

                if (context.TryGet("NoSubscribersFoundForMessage", out nosubscribers) && nosubscribers)
                {
                    Context.AddTrace(string.Format("No Subscribers found for message {0}", context.OutgoingLogicalMessage.MessageType.Name));
                }
            }

            public class Registration : RegisterStep
            {
                public Registration()
                    : base("SubscriptionTracer", typeof(SubscriptionTracer), "Traces the list of found subscribers")
                {
                    InsertBefore(WellKnownStep.DispatchMessageToTransport);
                }
            }
        }
    }
}