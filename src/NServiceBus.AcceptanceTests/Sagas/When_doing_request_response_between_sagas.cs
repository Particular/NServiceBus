
namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_doing_request_response_between_sagas : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_autocorrelate_the_response_back_to_the_requesting_saga()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new InitiateRequestingSaga { DataId = Guid.NewGuid() })))
                    .Done(c => c.DidRequestingSagaGetTheResponse)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.True(c.DidRequestingSagaGetTheResponse))
                    .Run(TimeSpan.FromSeconds(20));
        }

        public class Context : ScenarioContext
        {
            public bool DidRequestingSagaGetTheResponse { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {

            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class RequestingSaga : Saga<RequestingSaga.RequestingSagaData>, 
                IAmStartedByMessages<InitiateRequestingSaga>, 
                IHandleMessages<ResponseFromOtherSaga>
            {
                public Context Context { get; set; }

                public void Handle(InitiateRequestingSaga message)
                {
                    Data.DataId = message.DataId;
                    Bus.SendLocal(new RequestToRespondingSaga { DataId = message.DataId });
                }

                public void Handle(ResponseFromOtherSaga message)
                {
                    Context.DidRequestingSagaGetTheResponse = true;
                    MarkAsComplete();
                }

                public override void ConfigureHowToFindSaga()
                {
                    ConfigureMapping<InitiateRequestingSaga>(m => m.DataId).ToSaga(s => s.DataId);
                }
                public class RequestingSagaData : ContainSagaData
                {
                    [Unique]
                    public virtual Guid DataId { get; set; }
                }
            }

            public class RespondingSaga : Saga<RespondingSaga.RespondingSagaData>, 
                IAmStartedByMessages<RequestToRespondingSaga>
            {
                public Context Context { get; set; }

                public void Handle(RequestToRespondingSaga message)
                {
                    Bus.Reply(new ResponseFromOtherSaga { DataId = message.DataId });
                }

                public override void ConfigureHowToFindSaga()
                {
                }

                public class RespondingSagaData : ContainSagaData
                {
                }
            }
        }

        [Serializable]
        public class InitiateRequestingSaga : ICommand
        {
            public Guid DataId { get; set; }
        }

        [Serializable]
        public class RequestToRespondingSaga : ICommand
        {
            public Guid DataId { get; set; }
        }

        [Serializable]
        public class ResponseFromOtherSaga : IMessage
        {
            public Guid DataId { get; set; }
        }
    }
}
