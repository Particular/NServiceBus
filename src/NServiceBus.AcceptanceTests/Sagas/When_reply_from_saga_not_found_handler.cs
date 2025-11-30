namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_reply_from_saga_not_found_handler : NServiceBusAcceptanceTest
{
    [Test]
    public async Task It_should_invoke_message_handler()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithSaga>(b => b.When(session => session.SendLocal(new MessageToSaga())))
            .Done(c => c.ReplyReceived)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Logs.Any(m => m.Message.Equals("Could not find a started saga of 'NServiceBus.AcceptanceTests.Sagas.When_reply_from_saga_not_found_handler+EndpointWithSaga+NotFoundHandlerSaga' for message type 'NServiceBus.AcceptanceTests.Sagas.When_reply_from_saga_not_found_handler+MessageToSaga'.")), Is.True);
            Assert.That(context.ReplyReceived, Is.True);
        }
    }

    public class Context : ScenarioContext
    {
        public bool ReplyReceived { get; set; }
    }

    public class EndpointWithSaga : EndpointConfigurationBuilder
    {
        public EndpointWithSaga() => EndpointSetup<DefaultServer>();

        public class ReplyHandler(Context testContext) : IHandleMessages<Reply>
        {
            public Task Handle(Reply message, IMessageHandlerContext context)
            {
                testContext.ReplyReceived = true;

                return Task.CompletedTask;
            }
        }

        public class NotFoundHandlerSaga : Saga<NotFoundHandlerSaga.NotFoundHandlerSagaData>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
        {
            public Task Handle(StartSaga message, IMessageHandlerContext context) => throw new NotImplementedException();

            public Task Handle(MessageToSaga message, IMessageHandlerContext context) => Task.CompletedTask;

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NotFoundHandlerSagaData> mapper)
            {
                mapper.MapSaga(s => s.ContextId)
                    .ToMessage<StartSaga>(m => m.ContextId)
                    .ToMessage<MessageToSaga>(m => m.ContextId);

                mapper.ConfigureNotFoundHandler<SagaNotFoundDoingReply>();
            }

            public class NotFoundHandlerSagaData : ContainSagaData
            {
                public virtual Guid ContextId { get; set; }
            }
        }

        public class SagaNotFoundDoingReply : ISagaNotFoundHandler
        {
            public Task Handle(object message, IMessageProcessingContext context) => context.Reply(new Reply());
        }
    }

    public class StartSaga : ICommand
    {
        public Guid ContextId { get; set; }
    }

    public class MessageToSaga : ICommand
    {
        public Guid ContextId { get; set; }
    }

    public class Reply : IMessage;
}