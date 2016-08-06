namespace NServiceBus.AcceptanceTests.PipelineExt
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Transports;

    public class When_processing_a_message_in_a_satellite : NServiceBusAcceptanceTest
    {
        static string ReceiverAddress => Conventions.EndpointNamingConvention(typeof(Receiver));

        [Test]
        public async Task Forking_to_routing_stage_should_be_supported()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>()
                .WithEndpoint<Receiver>()
                .Done(c => c.MessageDelivered)
                .Run();

            Assert.IsTrue(context.MessageDelivered);
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c => c.EnableFeature<SatelliteFeature>());
            }

            class SatelliteFeature : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                    string satelliteAddress;
                    var pipe = context.AddSatellitePipeline("MySatellite", TransportTransactionMode.None, new PushRuntimeSettings(), "MySatellite", out satelliteAddress);
                    pipe.Register("MySatelliteBehavior", new SatelliteBehavior(), "This is my satellite behavior.");

                    context.RegisterStartupTask(new SenderTask(satelliteAddress));
                }

                class SatelliteBehavior : ForkConnector<ISatelliteProcessingContext, IRoutingContext>
                {
                    public override async Task Invoke(ISatelliteProcessingContext context, Func<Task> next, Func<IRoutingContext, Task> fork)
                    {
                        await next().ConfigureAwait(false);
                        var message = new OutgoingMessage(context.Message.MessageId, context.Message.Headers, context.Message.Body);
                        await fork(this.CreateRoutingContext(message, ReceiverAddress, context)).ConfigureAwait(false);
                    }
                }

                class SenderTask : FeatureStartupTask
                {
                    string satelliteAddress;

                    public SenderTask(string satelliteAddress)
                    {
                        this.satelliteAddress = satelliteAddress;
                    }

                    protected override Task OnStart(IMessageSession session)
                    {
                        var options = new SendOptions();
                        options.SetDestination(satelliteAddress);
                        return session.Send(new MyMessage(), options);
                    }

                    protected override Task OnStop(IMessageSession session)
                    {
                        return Task.FromResult(0);
                    }
                }
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }

            class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    Context.MessageDelivered = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class Context : ScenarioContext
        {
            public bool MessageDelivered { get; set; }
        }

        public class MyMessage : IMessage
        {
        }
    }
}