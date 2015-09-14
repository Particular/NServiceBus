namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Features;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    public class when_reply_from_saga_not_found_handler : NServiceBusAcceptanceTest
    {
        // related to NSB issue #2044
        [Test]
        public async Task It_should_invoke_message_handler()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Sender>(b => b.Given((bus, c) => bus.SendAsync(new MessageToSaga())))
                    .WithEndpoint<ReceiverWithSaga>()
                    .Done(c => c.ReplyReceived)
                    .Run();

            Assert.IsTrue(context.ReplyReceived);
        }

        public class Context : ScenarioContext
        {
            public bool ReplyReceived { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<MessageToSaga>(typeof(ReceiverWithSaga));
            }

            public class ReplyHandler : IHandleMessages<Reply>
            {
                public Context Context { get; set; }


                public Task Handle(Reply message)
                {
                    Context.ReplyReceived = true;

                    return Task.FromResult(0);
                }
            }
        }

        public class ReceiverWithSaga : EndpointConfigurationBuilder
        {
            public ReceiverWithSaga()
            {
                EndpointSetup<DefaultServer>(c => c.EnableFeature<TimeoutManager>());
            }

            public class NotFoundHandlerSaga1 : Saga<NotFoundHandlerSaga1.NotFoundHandlerSaga1Data>, IAmStartedByMessages<StartSaga1>, IHandleMessages<MessageToSaga>
            {

                public Task Handle(StartSaga1 message)
                {
                    Data.ContextId = message.ContextId;
                    return Task.FromResult(0);
                }

                public Task Handle(MessageToSaga message)
                {
                    return Task.FromResult(0);
                }

                public class NotFoundHandlerSaga1Data : ContainSagaData
                {
                    public virtual Guid ContextId { get; set; }
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NotFoundHandlerSaga1Data> mapper)
                {
                    mapper.ConfigureMapping<StartSaga1>(m => m.ContextId)
                        .ToSaga(s => s.ContextId);
                    mapper.ConfigureMapping<MessageToSaga>(m => m.ContextId)
                        .ToSaga(s => s.ContextId);
                }
            }

            public class SagaNotFound : IHandleSagaNotFound
            {
                public IBus Bus { get; set; }

                public Task Handle(object message)
                {
                    return Bus.ReplyAsync(new Reply());
                }
            }
        }

        [Serializable]
        public class StartSaga1 : ICommand
        {
            public Guid ContextId { get; set; }
        }

        [Serializable]
        public class MessageToSaga : ICommand
        {
            public Guid ContextId { get; set; }
        }

        [Serializable]
        public class Reply : IMessage
        {
        }
    }
}