namespace NServiceBus.AcceptanceTests.Core.Mutators
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using MessageMutator;
    using NUnit.Framework;

    public class When_mutating : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Context_should_be_populated()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When((session, c) => session.Send(new StartMessage())))
                .WithEndpoint<Receiver>()
                .Done(c => c.WasCalled)
                .Run(TimeSpan.FromHours(1));

            Assert.True(context.WasCalled, "The message handler should be called");
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.ConfigureRouting().RouteToEndpoint(typeof(StartMessage), typeof(Receiver));
                });
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(b => b.RegisterMessageMutator(new Mutator()));
            }

            public class StartMessageHandler : IHandleMessages<StartMessage>
            {
                public Task Handle(StartMessage message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new LoopMessage());
                }
            }

            public class LoopMessageHandler : IHandleMessages<LoopMessage>
            {
                public LoopMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(LoopMessage message, IMessageHandlerContext context)
                {
                    testContext.WasCalled = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }

            public class Mutator :
                IMutateIncomingMessages,
                IMutateIncomingTransportMessages,
                IMutateOutgoingMessages,
                IMutateOutgoingTransportMessages
            {
                public Task MutateIncoming(MutateIncomingMessageContext context)
                {
                    Assert.IsNotEmpty(context.Headers);
                    Assert.IsNotNull(context.Message);
                    return Task.FromResult(0);
                }

                public Task MutateIncoming(MutateIncomingTransportMessageContext context)
                {
                    Assert.IsNotEmpty(context.Headers);
                    Assert.IsNotNull(context.Body);
                    return Task.FromResult(0);
                }

                public Task MutateOutgoing(MutateOutgoingMessageContext context)
                {
                    Assert.IsNotEmpty(context.OutgoingHeaders);
                    Assert.IsNotNull(context.OutgoingMessage);
                    context.TryGetIncomingHeaders(out var incomingHeaders);
                    context.TryGetIncomingMessage(out var incomingMessage);
                    Assert.IsNotEmpty(incomingHeaders);
                    Assert.IsNotNull(incomingMessage);
                    return Task.FromResult(0);
                }

                public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
                {
                    Assert.IsNotEmpty(context.OutgoingHeaders);
                    Assert.IsNotNull(context.OutgoingBody);
                    context.TryGetIncomingHeaders(out var incomingHeaders);
                    context.TryGetIncomingMessage(out var incomingMessage);
                    Assert.IsNotEmpty(incomingHeaders);
                    Assert.IsNotNull(incomingMessage);
                    return Task.FromResult(0);
                }
            }
        }

        public class StartMessage : IMessage
        {
        }

        public class LoopMessage : IMessage
        {
        }
    }
}