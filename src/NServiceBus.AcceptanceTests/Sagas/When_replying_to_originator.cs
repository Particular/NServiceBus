namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_replying_to_originator : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_route_the_message_to_the_endpoint_starting_the_saga()
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

            public class RequestResponseRequestingSaga2 : Saga<RequestResponseRequestingSaga2.RequestResponseRequestingSagaData2>,
                IAmStartedByMessages<InitiateRequestingSaga>,
                IHandleMessages<ResponseFromOtherSaga>
            {
                public RequestResponseRequestingSaga2(Context context)
                {
                    testContext = context;
                }

                public Task Handle(InitiateRequestingSaga message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    return context.SendLocal(new RequestToRespondingSaga
                    {
                        SomeIdThatTheResponseSagaCanCorrelateBackToUs = Data.CorrIdForResponse
                    });
                }

                public Task Handle(ResponseFromOtherSaga message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.DidRequestingSagaGetTheResponse = true;

                    MarkAsComplete();

                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RequestResponseRequestingSagaData2> mapper)
                {
                    mapper.ConfigureMapping<InitiateRequestingSaga>(m => m.Id).ToSaga(s => s.CorrIdForResponse);
                    mapper.ConfigureMapping<ResponseFromOtherSaga>(m => m.SomeCorrelationId).ToSaga(s => s.CorrIdForResponse);
                }

                public class RequestResponseRequestingSagaData2 : ContainSagaData
                {
                    public virtual Guid CorrIdForResponse { get; set; }
                }

                Context testContext;
            }

            public class RequestResponseRespondingSaga2 : Saga<RequestResponseRespondingSaga2.RequestResponseRespondingSagaData2>,
                IAmStartedByMessages<RequestToRespondingSaga>,
                IHandleMessages<SendReplyFromNonInitiatingHandler>
            {
                public Task Handle(RequestToRespondingSaga message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    return context.SendLocal(new SendReplyFromNonInitiatingHandler
                    {
                        SagaIdSoWeCanCorrelate = Data.CorrIdForRequest
                    });
                }

                public Task Handle(SendReplyFromNonInitiatingHandler message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    //reply to originator must be used here since the sender of the incoming message is this saga and not the requesting saga
                    return ReplyToOriginator(context, new ResponseFromOtherSaga
                    {
                        SomeCorrelationId = Data.CorrIdForRequest
                    });
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RequestResponseRespondingSagaData2> mapper)
                {
                    mapper.ConfigureMapping<RequestToRespondingSaga>(m => m.SomeIdThatTheResponseSagaCanCorrelateBackToUs).ToSaga(s => s.CorrIdForRequest);
                    //this line is just needed so we can test the non-initiating handler case
                    mapper.ConfigureMapping<SendReplyFromNonInitiatingHandler>(m => m.SagaIdSoWeCanCorrelate).ToSaga(s => s.CorrIdForRequest);
                }

                public class RequestResponseRespondingSagaData2 : ContainSagaData
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

        public class SendReplyFromNonInitiatingHandler : ICommand
        {
            public Guid SagaIdSoWeCanCorrelate { get; set; }
        }
    }
}