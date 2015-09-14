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
                .WithEndpoint<SagaEndpoint>(b => b.Given(bus =>
                {
                    var message = new MessageWithSagaId();
                    var options = new SendOptions();

                    options.SetHeader(Headers.SagaId, Guid.NewGuid().ToString());
                    options.SetHeader(Headers.SagaType, typeof(MessageWithSagaIdSaga).AssemblyQualifiedName);
                    options.RouteToLocalEndpointInstance();
                    return bus.SendAsync(message, options);
                }))
                .Done(c => c.OtherSagaStarted)
                .Run();

            Assert.False(context.NotFoundHandlerCalled);
            Assert.True(context.OtherSagaStarted);
            Assert.False(context.MessageHandlerCalled);
            Assert.False(context.TimeoutHandlerCalled);
        }

        class MessageWithSagaIdSaga : Saga<MessageWithSagaIdSaga.MessageWithSagaIdSagaData>, IAmStartedByMessages<MessageWithSagaId>,
            IHandleTimeouts<MessageWithSagaId>,
            IHandleSagaNotFound
        {
            public Context Context { get; set; }

            public class MessageWithSagaIdSagaData : ContainSagaData
            {
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MessageWithSagaIdSagaData> mapper)
            {
            }

            public Task Handle(MessageWithSagaId message)
            {
                Context.MessageHandlerCalled = true;
                return Task.FromResult(0);
            }

            public Task Handle(object message)
            {
                Context.NotFoundHandlerCalled = true;
                return Task.FromResult(0);
            }

            public Task Timeout(MessageWithSagaId state)
            {
                Context.TimeoutHandlerCalled = true;
                return Task.FromResult(0);
            }
        }

        class MyOtherSaga : Saga<MyOtherSaga.SagaData>, IAmStartedByMessages<MessageWithSagaId>
        {
            public Context Context { get; set; }

            public Task Handle(MessageWithSagaId message)
            {
                Data.DataId = message.DataId;

                Context.OtherSagaStarted = true;

                return Task.FromResult(0);
            }
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<MessageWithSagaId>(m => m.DataId).ToSaga(s => s.DataId);
            }

            public class SagaData : ContainSagaData
            {
                public virtual Guid DataId { get; set; }
            }
        }


        class Context : ScenarioContext
        {
            public bool NotFoundHandlerCalled { get; set; }
            public bool MessageHandlerCalled { get; set; }
            public bool TimeoutHandlerCalled { get; set; }
            public bool OtherSagaStarted { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.EnableFeature<TimeoutManager>());
            }
        }

        public class MessageWithSagaId : IMessage
        {
            public Guid DataId { get; set; }
        }
    }
}