namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_sagas_share_timeout_messages : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_invoke_instance_that_requested_the_timeout()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(e => e.When(s => s.SendLocal(new StartSagaMessage()
                {
                    Id = Guid.NewGuid().ToString()
                })))
                .Done(c => c.Saga1ReceivedTimeout || c.Saga2ReceivedTimeout)
                .Run(TimeSpan.FromSeconds(30));

            Assert.IsTrue(context.Saga2ReceivedTimeout);
            Assert.IsFalse(context.Saga1ReceivedTimeout);
        }

        public class Context : ScenarioContext
        {
            public bool Saga1ReceivedTimeout { get; set; }
            public bool Saga2ReceivedTimeout { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<TimeoutManager>();
                });
            }

            public class Saga1 : Saga<Saga1.SagaData1>,
                IAmStartedByMessages<StartSagaMessage>,
                IHandleTimeouts<MySagaTimeout>
            {
                public Context Context { get; set; }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData1> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.Id).ToSaga(s => s.CorrelationProperty);
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                public Task Timeout(MySagaTimeout state, IMessageHandlerContext context)
                {
                    Context.Saga1ReceivedTimeout = true;
                    return Task.FromResult(0);
                }

                public class SagaData1 : ContainSagaData
                {
                    public string CorrelationProperty { get; set; }
                }

            }

            public class Saga2 : Saga<Saga2.SagaData2>, IAmStartedByMessages<StartSagaMessage>, IHandleTimeouts<MySagaTimeout>
            {
                public Context Context { get; set; }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData2> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.Id).ToSaga(s => s.CorrelationProperty);
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    return RequestTimeout<MySagaTimeout>(context, TimeSpan.FromSeconds(10));
                }

                public Task Timeout(MySagaTimeout state, IMessageHandlerContext context)
                {
                    Context.Saga2ReceivedTimeout = true;
                    return Task.FromResult(0);
                }
                public class SagaData2 : ContainSagaData
                {
                    public string CorrelationProperty { get; set; }
                }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public string Id { get; set; }
        }

        public class MySagaTimeout
        {
        }
    }
}