namespace NServiceBus.AcceptanceTests.Core.Mutators
{
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
                .Done(c => c.NewMessageHandlerCalled || c.OriginalMessageHandlerCalled)
                .Run();

            Assert.IsTrue(context.NewMessageHandlerCalled);
            Assert.IsTrue(context.NewMessageSagaHandlerCalled);
            Assert.IsFalse(context.OriginalMessageHandlerCalled);
            Assert.IsFalse(context.OriginalMessageSagaHandlerCalled);
        }

        public class Context : ScenarioContext
        {
            public bool OriginalMessageHandlerCalled { get; set; }
            public bool NewMessageHandlerCalled { get; set; }
            public bool OriginalMessageSagaHandlerCalled { get; set; }
            public bool NewMessageSagaHandlerCalled { get; set; }
        }

        public class MutatorEndpoint : EndpointConfigurationBuilder
        {
            public MutatorEndpoint()
            {
                EndpointSetup<DefaultServer>(e => e.RegisterMessageMutator(new MessageMutator()));
            }

            public class MessageMutator : IMutateIncomingMessages
            {
                public Task MutateIncoming(MutateIncomingMessageContext context)
                {
                    var original = (OriginalMessage)context.Message;
                    context.Message = new NewMessage { SomeId = original.SomeId };
                    return Task.FromResult(0);
                }
            }

            public class OriginalMessageHandler : IHandleMessages<OriginalMessage>
            {
                public OriginalMessageHandler(Context testContext)
                {
                    TestContext = testContext;
                }

                public Task Handle(OriginalMessage message, IMessageHandlerContext context)
                {
                    TestContext.OriginalMessageHandlerCalled = true;
                    return Task.FromResult(0);
                }

                Context TestContext;
            }

            public class NewMessageHandler : IHandleMessages<NewMessage>
            {
                public NewMessageHandler(Context testContext)
                {
                    TestContext = testContext;
                }

                public Task Handle(NewMessage message, IMessageHandlerContext context)
                {
                    TestContext.NewMessageHandlerCalled = true;
                    return Task.FromResult(0);
                }

                Context TestContext;
            }

            public class Saga : Saga<SagaData>, IAmStartedByMessages<OriginalMessage>, IAmStartedByMessages<NewMessage>
            {
                public Saga(Context testContext)
                {
                    TestContext = testContext;
                }

                public Task Handle(NewMessage message, IMessageHandlerContext context)
                {
                    TestContext.NewMessageSagaHandlerCalled = true;
                    return Task.FromResult(0);
                }

                public Task Handle(OriginalMessage message, IMessageHandlerContext context)
                {
                    TestContext.OriginalMessageSagaHandlerCalled = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
                {
                    mapper.ConfigureMapping<OriginalMessage>(msg => msg.SomeId).ToSaga(saga => saga.SomeId);
                    mapper.ConfigureMapping<NewMessage>(msg => msg.SomeId).ToSaga(saga => saga.SomeId);
                }

                Context TestContext;
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
}