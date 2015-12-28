namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Features;

    public class When_doing_request_response_between_sagas : NServiceBusAcceptanceTest
    {
        public class Context : ScenarioContext
        {
            public bool DidRequestingSagaGetTheResponse { get; set; }
            public bool ReplyFromTimeout { get; set; }
            public bool ReplyFromNonInitiatingHandler { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {

            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }

            public class RequestResponseRequestingSaga : Saga<RequestResponseRequestingSaga.RequestResponseRequestingSagaData>,
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

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RequestResponseRequestingSagaData> mapper)
                {
                    mapper.ConfigureMapping<InitiateRequestingSaga>(m => m.Id).ToSaga(s => s.CorrIdForResponse);
                    mapper.ConfigureMapping<ResponseFromOtherSaga>(m => m.SomeCorrelationId).ToSaga(s => s.CorrIdForResponse);
                }

                public class RequestResponseRequestingSagaData : ContainSagaData
                {
                    public virtual Guid CorrIdForResponse { get; set; } //wont be needed in the future
                }
            }

            public class RequestResponseRespondingSaga : Saga<RequestResponseRespondingSaga.RequestResponseRespondingSagaData>,
                IAmStartedByMessages<RequestToRespondingSaga>,
                IHandleTimeouts<RequestResponseRespondingSaga.DelayReply>,
                IHandleMessages<SendReplyFromNonInitiatingHandler>
            {
                public Context TestContext { get; set; }

                public async Task Handle(RequestToRespondingSaga message, IMessageHandlerContext context)
                {
                    if (TestContext.ReplyFromNonInitiatingHandler)
                    {
                        await context.SendLocal(new SendReplyFromNonInitiatingHandler { SagaIdSoWeCanCorrelate = Data.Id });
                    }

                    if (TestContext.ReplyFromTimeout)
                    {
                        await RequestTimeout<DelayReply>(context, TimeSpan.FromMilliseconds(1));
                    }

                    // Both reply and reply to originator work here since the sender of the incoming message is the requesting saga
                    // also note we don't set the correlation ID since auto correlation happens to work for this special case 
                    // where we reply from the first handler
                    await context.Reply(new ResponseFromOtherSaga());
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RequestResponseRespondingSagaData> mapper)
                {
                    mapper.ConfigureMapping<RequestToRespondingSaga>(m => m.SomeIdThatTheResponseSagaCanCorrelateBackToUs).ToSaga(s => s.CorrIdForRequest);
                    //this line is just needed so we can test the non initiating handler case
                    mapper.ConfigureMapping<SendReplyFromNonInitiatingHandler>(m => m.SagaIdSoWeCanCorrelate).ToSaga(s => s.CorrIdForRequest);
                }

                public class RequestResponseRespondingSagaData : ContainSagaData
                {
                    public virtual Guid CorrIdForRequest { get; set; }
                }

                public class DelayReply { }

                public Task Timeout(DelayReply state, IMessageHandlerContext context)
                {
                    return SendReply(context);
                }

                public Task Handle(SendReplyFromNonInitiatingHandler message, IMessageHandlerContext context)
                {
                    return SendReply(context);
                }

                Task SendReply(IMessageHandlerContext context)
                {
                    //reply to originator must be used here since the sender of the incoming message the timeoutmanager and not the requesting saga
                    return ReplyToOriginator(context, new ResponseFromOtherSaga //change this line to Bus.Reply(new ResponseFromOtherSaga  and see it fail
                    {
                        SomeCorrelationId = Data.CorrIdForRequest //wont be needed in the future
                    });
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