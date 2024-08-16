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
            .Done(c => c.WasCalled)
            .Run(TimeSpan.FromHours(1));

        Assert.That(context.WasCalled, Is.True, "The message handler should be called");
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
                return Task.CompletedTask;
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
                Assert.Multiple(() =>
                {
                    Assert.That(context.Headers, Is.Not.Empty);
                    Assert.That(context.Message, Is.Not.Null);
                });
                return Task.CompletedTask;
            }

            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(context.Headers, Is.Not.Empty);
                    Assert.That(context.Body, Is.Not.EqualTo(default(ReadOnlyMemory<byte>)));
                });
                return Task.CompletedTask;
            }

            public Task MutateOutgoing(MutateOutgoingMessageContext context)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(context.OutgoingHeaders, Is.Not.Empty);
                    Assert.That(context.OutgoingMessage, Is.Not.Null);
                });
                context.TryGetIncomingHeaders(out var incomingHeaders);
                context.TryGetIncomingMessage(out var incomingMessage);
                Assert.Multiple(() =>
                {
                    Assert.That(incomingHeaders, Is.Not.Empty);
                    Assert.That(incomingMessage, Is.Not.Null);
                });
                return Task.CompletedTask;
            }

            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(context.OutgoingHeaders, Is.Not.Empty);
                    Assert.That(context.OutgoingBody, Is.Not.EqualTo(default(ReadOnlyMemory<byte>)));
                });
                context.TryGetIncomingHeaders(out var incomingHeaders);
                context.TryGetIncomingMessage(out var incomingMessage);
                Assert.Multiple(() =>
                {
                    Assert.That(incomingHeaders, Is.Not.Empty);
                    Assert.That(incomingMessage, Is.Not.Null);
                });
                return Task.CompletedTask;
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