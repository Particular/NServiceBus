namespace NServiceBus.AcceptanceTests.Core.Mutators;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using MessageMutator;
using NUnit.Framework;

public class When_incoming_mutator_changes_message_type : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_invoke_handlers_for_new_message_type()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<MutatorEndpoint>(e => e
                .When(s => s.SendLocal(new OriginalMessage { SomeId = "TestId" })))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.NewMessageHandlerCalled, Is.True);
            Assert.That(context.NewMessageSagaHandlerCalled, Is.True);
            Assert.That(context.OriginalMessageHandlerCalled, Is.False);
            Assert.That(context.OriginalMessageSagaHandlerCalled, Is.False);
        }
    }

    public class Context : ScenarioContext
    {
        public bool OriginalMessageHandlerCalled { get; set; }
        public bool NewMessageHandlerCalled { get; set; }
        public bool OriginalMessageSagaHandlerCalled { get; set; }
        public bool NewMessageSagaHandlerCalled { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(NewMessageHandlerCalled, NewMessageSagaHandlerCalled);
    }

    public class MutatorEndpoint : EndpointConfigurationBuilder
    {
        public MutatorEndpoint() => EndpointSetup<DefaultServer>(e => e.RegisterMessageMutator(new MessageMutator()));

        public class MessageMutator : IMutateIncomingMessages
        {
            public Task MutateIncoming(MutateIncomingMessageContext context)
            {
                var original = (OriginalMessage)context.Message;
                context.Message = new NewMessage { SomeId = original.SomeId };
                return Task.CompletedTask;
            }
        }

        [Handler]
        public class OriginalMessageHandler(Context testContext) : IHandleMessages<OriginalMessage>
        {
            public Task Handle(OriginalMessage message, IMessageHandlerContext context)
            {
                testContext.OriginalMessageHandlerCalled = true;
                testContext.MarkAsFailed(new InvalidOperationException("Should not be called"));
                return Task.CompletedTask;
            }
        }

        [Handler]
        public class NewMessageHandler(Context testContext) : IHandleMessages<NewMessage>
        {
            public Task Handle(NewMessage message, IMessageHandlerContext context)
            {
                testContext.NewMessageHandlerCalled = true;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }

        [Saga]
        public class Saga(Context testContext)
            : Saga<SagaData>, IAmStartedByMessages<OriginalMessage>, IAmStartedByMessages<NewMessage>
        {
            public Task Handle(NewMessage message, IMessageHandlerContext context)
            {
                testContext.NewMessageSagaHandlerCalled = true;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }

            public Task Handle(OriginalMessage message, IMessageHandlerContext context)
            {
                testContext.OriginalMessageSagaHandlerCalled = true;
                testContext.MarkAsFailed(new InvalidOperationException("Should not be called"));
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
                mapper.MapSaga(s => s.SomeId)
                    .ToMessage<OriginalMessage>(msg => msg.SomeId)
                    .ToMessage<NewMessage>(msg => msg.SomeId);
        }

        public class SagaData : ContainSagaData
        {
            public virtual string SomeId { get; set; }
        }
    }

    public class OriginalMessage : ICommand
    {
        public string SomeId { get; set; }
    }

    public class NewMessage : ICommand
    {
        public string SomeId { get; set; }
    }
}