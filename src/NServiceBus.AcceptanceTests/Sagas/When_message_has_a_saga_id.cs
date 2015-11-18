namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    public class When_message_has_a_saga_id : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_start_a_new_saga_if_not_found()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b => b.When(bus =>
                {
                    var message = new MessageWithSagaId
                    {
                        DataId = Guid.NewGuid()
                    };
                    var options = new SendOptions();

                    options.SetHeader(Headers.SagaId, Guid.NewGuid().ToString());
                    options.SetHeader(Headers.SagaType, typeof(SagaEndpoint.MessageWithSagaIdSaga).AssemblyQualifiedName);
                    options.RouteToLocalEndpointInstance();
                    return bus.Send(message, options);
                }))
                .Done(c => c.Done)
                .Run();

            Assert.True(context.NotFoundHandlerCalled);
            Assert.False(context.MessageHandlerCalled);
            Assert.False(context.TimeoutHandlerCalled);
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
                EndpointSetup<DefaultServer>(c => c.EnableFeature<TimeoutManager>());
            }

            public class MessageWithSagaIdSaga : Saga<MessageWithSagaIdSaga.MessageWithSagaIdSagaData>,
                IAmStartedByMessages<MessageWithSagaId>,
                IHandleTimeouts<MessageWithSagaId>,
                IHandleSagaNotFound
            {
                public Context TestContext { get; set; }

                public Task Handle(MessageWithSagaId message, IMessageHandlerContext context)
                {
                    TestContext.MessageHandlerCalled = true;
                    return Task.FromResult(0);
                }

                public Task Handle(object message, IMessageProcessingContext context)
                {
                    TestContext.NotFoundHandlerCalled = true;
                    return Task.FromResult(0);
                }

                public Task Timeout(MessageWithSagaId state, IMessageHandlerContext context)
                {
                    TestContext.TimeoutHandlerCalled = true;
                    return Task.FromResult(0);
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
            }

            class MessageWithSagaIdHandler:IHandleMessages<MessageWithSagaId>
            {
                public Context TestContext { get; set; }
                public Task Handle(MessageWithSagaId message, IMessageHandlerContext context)
                {
                    TestContext.Done = true;

                    return Task.FromResult(0);
                }
            }
        }

        public class MessageWithSagaId : IMessage
        {
            public Guid DataId { get; set; }
        }
    }
}