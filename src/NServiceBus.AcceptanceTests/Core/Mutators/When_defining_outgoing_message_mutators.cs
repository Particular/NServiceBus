namespace NServiceBus.AcceptanceTests.Core.Mutators;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using MessageMutator;
using NUnit.Framework;

public class When_defining_outgoing_message_mutators : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_be_applied_to_outgoing_messages()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new Message())))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.TransportMutatorCalled, Is.True);
            Assert.That(context.OtherTransportMutatorCalled, Is.True);
            Assert.That(context.MessageMutatorCalled, Is.True);
        }
    }

    public class Context : ScenarioContext
    {
        public bool MessageProcessed { get; set; }
        public bool TransportMutatorCalled { get; set; }
        public bool OtherTransportMutatorCalled { get; set; }
        public bool MessageMutatorCalled { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() =>
            EndpointSetup<DefaultServer, Context>((config, context) =>
            {
                config.RegisterMessageMutator(new TransportMutator(context));
                config.RegisterMessageMutator(new OtherTransportMutator(context));
                config.RegisterMessageMutator(new MessageMutator(context));
            });

        class TransportMutator(Context testContext) : IMutateOutgoingTransportMessages
        {
            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
            {
                testContext.TransportMutatorCalled = true;
                return Task.CompletedTask;
            }
        }

        class OtherTransportMutator(Context testContext) : IMutateOutgoingTransportMessages
        {
            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
            {
                testContext.OtherTransportMutatorCalled = true;
                return Task.CompletedTask;
            }
        }

        class MessageMutator(Context testContext) : IMutateOutgoingMessages
        {
            public Task MutateOutgoing(MutateOutgoingMessageContext context)
            {
                testContext.MessageMutatorCalled = true;
                return Task.CompletedTask;
            }
        }

        class Handler(Context testContext) : IHandleMessages<Message>
        {
            public Task Handle(Message message, IMessageHandlerContext context)
            {
                testContext.MessageProcessed = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class Message : ICommand;
}