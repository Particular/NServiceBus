namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.MessageMutator;
    using NUnit.Framework;

    public class When_mutating : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Context_should_be_populated()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Sender>(b => b.When((bus, c) => bus.Send(new StartMessage())))
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
                EndpointSetup<DefaultServer>()
                    .AddMapping<StartMessage>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(
                    b => b.RegisterComponents(r => r.ConfigureComponent<Mutator>(DependencyLifecycle.InstancePerCall)));
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
                Context testContext;
                public LoopMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }
                public Task Handle(LoopMessage message, IMessageHandlerContext context)
                {
                    testContext.WasCalled = true;
                    return Task.FromResult(0);
                }
            }

            public class Mutator:
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
                    IReadOnlyDictionary<string, string> incomingHeaders;
                    context.TryGetIncomingHeaders(out incomingHeaders);
                    object incomingmessage;
                    context.TryGetIncomingMessage(out incomingmessage);
                    Assert.IsNotEmpty(incomingHeaders);
                    Assert.IsNotNull(incomingmessage);
                    return Task.FromResult(0);
                }

                public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
                {
                    Assert.IsNotEmpty(context.OutgoingHeaders);
                    Assert.IsNotNull(context.OutgoingBody);
                    IReadOnlyDictionary<string, string> incomingHeaders;
                    context.TryGetIncomingHeaders(out incomingHeaders);
                    object incomingmessage;
                    context.TryGetIncomingMessage(out incomingmessage);
                    Assert.IsNotEmpty(incomingHeaders);
                    Assert.IsNotNull(incomingmessage);
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
