namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_doing_request_response_between_sagas_first_handler_responding : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_autocorrelate_the_response_back_to_the_requesting_saga_from_the_first_handler()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new InitiateRequestingSaga())))
                .Done(c => c.DidRequestingSagaGetTheResponse)
                .Run();

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

            public class RequestResponseRequestingSaga1 : Saga<RequestResponseRequestingSaga1.RequestResponseRequestingSagaData1>,
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

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RequestResponseRequestingSagaData1> mapper)
                {
                    mapper.ConfigureMapping<InitiateRequestingSaga>(m => m.Id).ToSaga(s => s.CorrIdForResponse);
                    mapper.ConfigureMapping<ResponseFromOtherSaga>(m => m.SomeCorrelationId).ToSaga(s => s.CorrIdForResponse);
                }

                public class RequestResponseRequestingSagaData1 : ContainSagaData
                {
                    public virtual Guid CorrIdForResponse { get; set; } //wont be needed in the future
                }
            }

            public class RequestResponseRespondingSaga1 : Saga<RequestResponseRespondingSaga1.RequestResponseRespondingSagaData1>,
                IAmStartedByMessages<RequestToRespondingSaga>
            {
                public Context TestContext { get; set; }

                public Task Handle(RequestToRespondingSaga message, IMessageHandlerContext context)
                {
                    // Both reply and reply to originator work here since the sender of the incoming message is the requesting saga
                    // also note we don't set the correlation ID since auto correlation happens to work for this special case
                    // where we reply from the first handler
                    return context.Reply(new ResponseFromOtherSaga());
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RequestResponseRespondingSagaData1> mapper)
                {
                    mapper.ConfigureMapping<RequestToRespondingSaga>(m => m.SomeIdThatTheResponseSagaCanCorrelateBackToUs).ToSaga(s => s.CorrIdForRequest);
                }

                public class RequestResponseRespondingSagaData1 : ContainSagaData
                {
                    public virtual Guid CorrIdForRequest { get; set; }
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
