namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_doing_request_response_between_sagas_with_timeout : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_autocorrelate_the_response_back_to_the_requesting_saga_from_timeouts()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new InitiateRequestingSaga())))
                .Done(c => c.DidRequestingSagaGetTheResponse)
                .Run(TimeSpan.FromSeconds(15));

            Assert.True(context.DidRequestingSagaGetTheResponse);
        }

        public class Context : ScenarioContext
        {
            public bool DidRequestingSagaGetTheResponse { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }

            public class RequestResponseRequestingSaga3 : Saga<RequestResponseRequestingSaga3.RequestResponseRequestingSagaData3>,
                IAmStartedByMessages<InitiateRequestingSaga>,
                IHandleMessages<ResponseFromOtherSaga>
            {
                public Context TestContext { get; set; }

                public Task Handle(InitiateRequestingSaga message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new RequestToRespondingSaga
                    {
                        SomeIdThatTheResponseSagaCanCorrelateBackToUs = Data.CorrIdForResponse //wont be needed in the future
                    });
                }

                public Task Handle(ResponseFromOtherSaga message, IMessageHandlerContext context)
                {
                    TestContext.DidRequestingSagaGetTheResponse = true;

                    MarkAsComplete();

                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RequestResponseRequestingSagaData3> mapper)
                {
                    mapper.ConfigureMapping<InitiateRequestingSaga>(m => m.Id).ToSaga(s => s.CorrIdForResponse);
                    mapper.ConfigureMapping<ResponseFromOtherSaga>(m => m.SomeCorrelationId).ToSaga(s => s.CorrIdForResponse);
                }

                public class RequestResponseRequestingSagaData3 : ContainSagaData
                {
                    public virtual Guid CorrIdForResponse { get; set; } //wont be needed in the future
                }
            }

            public class RequestResponseRespondingSaga3 : Saga<RequestResponseRespondingSaga3.RequestResponseRespondingSagaData3>,
                IAmStartedByMessages<RequestToRespondingSaga>,
                IHandleTimeouts<RequestResponseRespondingSaga3.DelayReply>
            {
                public Context TestContext { get; set; }

                public Task Handle(RequestToRespondingSaga message, IMessageHandlerContext context)
                {
                    return RequestTimeout<DelayReply>(context, TimeSpan.FromMilliseconds(1));
                }

                public Task Timeout(DelayReply state, IMessageHandlerContext context)
                {
                    //reply to originator must be used here since the sender of the incoming message is the TimeoutManager and not the requesting saga
                    return ReplyToOriginator(context, new ResponseFromOtherSaga //change this line to Bus.Reply(new ResponseFromOtherSaga and see it fail
                    {
                        SomeCorrelationId = Data.CorrIdForRequest //wont be needed in the future
                    });
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RequestResponseRespondingSagaData3> mapper)
                {
                    mapper.ConfigureMapping<RequestToRespondingSaga>(m => m.SomeIdThatTheResponseSagaCanCorrelateBackToUs).ToSaga(s => s.CorrIdForRequest);
                }


                public class RequestResponseRespondingSagaData3 : ContainSagaData
                {
                    public virtual Guid CorrIdForRequest { get; set; }
                }

                public class DelayReply
                {
                }
            }
        }

        public class InitiateRequestingSaga : ICommand
        {
            public InitiateRequestingSaga()
            {
                Id = Guid.NewGuid();
            }

            public Guid Id { get; set; }
        }

        public class RequestToRespondingSaga : ICommand
        {
            public Guid SomeIdThatTheResponseSagaCanCorrelateBackToUs { get; set; }
        }

        public class ResponseFromOtherSaga : IMessage
        {
            public Guid SomeCorrelationId { get; set; }
        }
    }
}
