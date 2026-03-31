namespace NServiceBus.AcceptanceTests.Core.Mutators;

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
            .Run();

        Assert.That(context.WasCalled, Is.True, "The message handler should be called");
    }

    public class Context : ScenarioContext
    {
        public bool WasCalled { get; set; }
    }

    public class Sender : EndpointConfigurationBuilder
    {
        public Sender() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.ConfigureRouting().RouteToEndpoint(typeof(StartMessage), typeof(Receiver));
            });
    }

    public class Receiver : EndpointConfigurationBuilder
    {
        public Receiver() => EndpointSetup<DefaultServer>(b => b.RegisterMessageMutator(new Mutator()));

        [Handler]
        public class StartMessageHandler : IHandleMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context) => context.SendLocal(new LoopMessage());
        }

        [Handler]
        public class LoopMessageHandler(Context testContext) : IHandleMessages<LoopMessage>
        {
            public Task Handle(LoopMessage message, IMessageHandlerContext context)
            {
                testContext.WasCalled = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }

        public class Mutator :
            IMutateIncomingMessages,
            IMutateIncomingTransportMessages,
            IMutateOutgoingMessages,
            IMutateOutgoingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingMessageContext context)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(context.Headers, Is.Not.Empty);
                    Assert.That(context.Message, Is.Not.Null);
                }
                return Task.CompletedTask;
            }

            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(context.Headers, Is.Not.Empty);
                    Assert.That(context.Body, Is.Not.EqualTo(default(ReadOnlyMemory<byte>)));
                }
                return Task.CompletedTask;
            }

            public Task MutateOutgoing(MutateOutgoingMessageContext context)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(context.OutgoingHeaders, Is.Not.Empty);
                    Assert.That(context.OutgoingMessage, Is.Not.Null);
                }
                context.TryGetIncomingHeaders(out var incomingHeaders);
                context.TryGetIncomingMessage(out var incomingMessage);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(incomingHeaders, Is.Not.Empty);
                    Assert.That(incomingMessage, Is.Not.Null);
                }
                return Task.CompletedTask;
            }

            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(context.OutgoingHeaders, Is.Not.Empty);
                    Assert.That(context.OutgoingBody, Is.Not.EqualTo(default(ReadOnlyMemory<byte>)));
                }
                context.TryGetIncomingHeaders(out var incomingHeaders);
                context.TryGetIncomingMessage(out var incomingMessage);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(incomingHeaders, Is.Not.Empty);
                    Assert.That(incomingMessage, Is.Not.Null);
                }
                return Task.CompletedTask;
            }
        }
    }

    public class StartMessage : IMessage;

    public class LoopMessage : IMessage;
}