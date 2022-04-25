namespace NServiceBus.AcceptanceTests.Core.Routing
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_sending_from_outgoing_pipeline : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_default_routing_when_empty_send_options()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointA>(e => e
                    .CustomConfig(c =>
                    {
                        c.Pipeline.Register(new SendingBehaviorUsingDefaultRouting(), "outgoing pipeline behavior sending messages");
                        c.ConfigureRouting().RouteToEndpoint(typeof(BehaviorMessage), typeof(EndpointB));
                    })
                    .When(s => s.SendLocal(new LocalMessage())))
                .WithEndpoint<EndpointB>()
                .Done(c => c.LocalMessageReceived && c.BehaviorMessageReceived)
                .Run(TimeSpan.FromSeconds(15));

            Assert.True(context.LocalMessageReceived);
            Assert.True(context.BehaviorMessageReceived);
        }

        [Test]
        public async Task Should_apply_send_options_routing()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointA>(e => e
                    .CustomConfig(c =>
                    {
                        c.Pipeline.Register(new SendingBehaviorUsingRoutingOverride(), "outgoing pipeline behavior sending messages");
                    })
                    .When(s => s.SendLocal(new LocalMessage())))
                .WithEndpoint<EndpointB>()
                .Done(c => c.LocalMessageReceived && c.BehaviorMessageReceived)
                .Run(TimeSpan.FromSeconds(15));

            Assert.True(context.LocalMessageReceived);
            Assert.True(context.BehaviorMessageReceived);
        }

        public class Context : ScenarioContext
        {
            public bool BehaviorMessageReceived { get; set; }
            public bool LocalMessageReceived { get; set; }
        }

        public class EndpointA : EndpointConfigurationBuilder
        {
            public EndpointA() => EndpointSetup<DefaultServer>();

            public class LocalMessageHandler : IHandleMessages<LocalMessage>
            {
                Context testContext;

                public LocalMessageHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(LocalMessage message, IMessageHandlerContext context)
                {
                    testContext.LocalMessageReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class EndpointB : EndpointConfigurationBuilder
        {
            public EndpointB() => EndpointSetup<DefaultServer>();

            public class BehaviorMessageHandler : IHandleMessages<BehaviorMessage>
            {
                Context testContext;

                public BehaviorMessageHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(BehaviorMessage message, IMessageHandlerContext context)
                {
                    testContext.BehaviorMessageReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class SendingBehaviorUsingDefaultRouting : Behavior<IOutgoingLogicalMessageContext>
        {
            public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
            {
                await next();
                if (context.Message.MessageType != typeof(BehaviorMessage)) // prevent infinite loop
                {
                    await context.Send(new BehaviorMessage(), new SendOptions()); // use empty SendOptions
                }
            }
        }

        public class SendingBehaviorUsingRoutingOverride : Behavior<IOutgoingLogicalMessageContext>
        {
            public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
            {
                await next();
                if (context.Message.MessageType != typeof(BehaviorMessage)) // prevent infinite loop
                {
                    var sendOptions = new SendOptions();
                    sendOptions.SetDestination(Conventions.EndpointNamingConvention(typeof(EndpointB))); // Configure routing on SendOptions
                    await context.Send(new BehaviorMessage(), sendOptions);
                }
            }
        }

        public class LocalMessage : IMessage
        {
        }

        public class BehaviorMessage : IMessage
        {
        }
    }
}