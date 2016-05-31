namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Extensibility;
    using Features;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Pipeline;
    using Transports;

    public class When_receiving_control_message_with_body : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_treat_it_as_control_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<TestingEndpoint>()
                .Done(c => c.ControlMessageProcessed || c.ControlMessageFailed)
                .Run();

            Assert.IsTrue(context.ControlMessageProcessed);
        }

        public class Context : ScenarioContext
        {
            public bool ControlMessageProcessed { get; set; }
            public bool ControlMessageFailed { get; set; }
        }

        class V33ControlMessageSimulator : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(b => new V33ControlMessageSimulatorTask(b.Build<IDispatchMessages>()));
            }

            class V33ControlMessageSimulatorTask : FeatureStartupTask
            {
                readonly IDispatchMessages dispatcher;

                public V33ControlMessageSimulatorTask(IDispatchMessages dispatcher)
                {
                    this.dispatcher = dispatcher;
                }

                protected override Task OnStart(IMessageSession session)
                {
                    // Simulating a v3.3 control message
                    var body = Encoding.UTF8.GetBytes(@"<?xml version=""1.0""?>
<Messages xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Unicast.Transport"">
    <CompletionMessage>
        <ErrorCode>5</ErrorCode>
    </CompletionMessage>
</Messages>");
                    var outgoingMessage = new OutgoingMessage("0dac4ec2-a0ed-42ee-a306-ff191322d59d\\47283703", new Dictionary<string, string>
                    {
                        {"NServiceBus.ControlMessage", "True"},
                        {"NServiceBus.ReturnMessage.ErrorCode", "5"},
                        {"NServiceBus.ContentType", "text/xml"}
                    }, body);

                    var endpoint = Conventions.EndpointNamingConvention(typeof(TestingEndpoint));
                    return dispatcher.Dispatch(new TransportOperations(new TransportOperation(outgoingMessage, new UnicastAddressTag(endpoint))), new ContextBag());
                }

                protected override Task OnStop(IMessageSession session)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class TestingEndpoint : EndpointConfigurationBuilder
        {
            public TestingEndpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.UseTransport<MsmqTransport>();
                    config.Pipeline.Register(typeof(AssertBehavior), "Asserts message was processed without any failures");
                    config.EnableFeature<V33ControlMessageSimulator>();
                });
            }

            public class AssertBehavior : Behavior<IIncomingPhysicalMessageContext>
            {
                public AssertBehavior(Context context)
                {
                    this.context = context;
                }

                public override async Task Invoke(IIncomingPhysicalMessageContext c, Func<Task> next)
                {
                    try
                    {
                        await next().ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        context.ControlMessageFailed = true;
                        return;
                    }

                    context.ControlMessageProcessed = true;
                }

                readonly Context context;
            }
        }
    }
}