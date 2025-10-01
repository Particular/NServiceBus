namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus.Sagas;
using NUnit.Framework;

public class When_message_has_a_saga_id : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_start_a_new_saga_if_not_found()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<SagaEndpoint>(b => b.When(session =>
            {
                var message = new MessageWithSagaId
                {
                    DataId = Guid.NewGuid()
                };
                var options = new SendOptions();

                options.SetHeader(Headers.SagaId, Guid.NewGuid().ToString());
                options.SetHeader(Headers.SagaType, typeof(SagaEndpoint.MessageWithSagaIdSaga).AssemblyQualifiedName);
                options.RouteToThisEndpoint();
                return session.Send(message, options);
            }))
            .Done(c => c.Done)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.NotFoundHandlerCalled, Is.True);
            Assert.That(context.MessageHandlerCalled, Is.False);
            Assert.That(context.TimeoutHandlerCalled, Is.False);
        }
    }

    public class Context : ScenarioContext
    {
        public bool NotFoundHandlerCalled { get; set; }
        public bool MessageHandlerCalled { get; set; }
        public bool TimeoutHandlerCalled { get; set; }
        public bool OtherSagaStarted { get; set; }
        public bool Done { get; set; }
    }

    public class SagaEndpoint : EndpointConfigurationBuilder
    {
        public SagaEndpoint()
        {
            EndpointSetup<DefaultServer>();
        }

        public class MessageWithSagaIdSaga : Saga<MessageWithSagaIdSaga.MessageWithSagaIdSagaData>,
            IAmStartedByMessages<MessageWithSagaId>,
            IHandleTimeouts<MessageWithSagaId>,
            IHandleSagaNotFound
        {
            public MessageWithSagaIdSaga(Context context)
            {
                testContext = context;
            }

            public Task Handle(MessageWithSagaId message, IMessageHandlerContext context)
            {
                testContext.MessageHandlerCalled = true;
                return Task.CompletedTask;
            }

            public Task Handle(object message, IMessageProcessingContext context)
            {
                testContext.NotFoundHandlerCalled = true;
                return Task.CompletedTask;
            }

            public Task Timeout(MessageWithSagaId state, IMessageHandlerContext context)
            {
                testContext.TimeoutHandlerCalled = true;
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MessageWithSagaIdSagaData> mapper)
            {
                mapper.ConfigureMapping<MessageWithSagaId>(m => m.DataId)
                    .ToSaga(s => s.DataId);
            }

            public class MessageWithSagaIdSagaData : ContainSagaData
            {
                public virtual Guid DataId { get; set; }
            }

            Context testContext;
        }

        class MessageWithSagaIdHandler : IHandleMessages<MessageWithSagaId>
        {
            public MessageWithSagaIdHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MessageWithSagaId message, IMessageHandlerContext context)
            {
                testContext.Done = true;

                return Task.CompletedTask;
            }

            Context testContext;
        }
    }

    public class MessageWithSagaId : IMessage
    {
        public Guid DataId { get; set; }
    }
}