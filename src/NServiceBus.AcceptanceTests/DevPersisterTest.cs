namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using Persistence;

    public class DevPersisterTest : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task name()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(e => e.When(s => s.SendLocal(new StartSagaMessage()
                {
                    Id = "42"
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
                    c.UsePersistence<DevelopmentPersistence, StorageType.Sagas>();
                    c.EnableFeature<TimeoutManager>();
                });
            }

            public class Saga1 : Saga<SagaData1>,
                IAmStartedByMessages<StartSagaMessage>,
                IHandleTimeouts<SagaTimeoutMessage>
            {
                public Context Context { get; set; }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData1> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.Id).ToSaga(s => s.CorrelationProperty);
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Console.WriteLine("started saga1");
                    return Task.FromResult(0);
                }

                public Task Timeout(SagaTimeoutMessage state, IMessageHandlerContext context)
                {
                    Console.WriteLine("Saga1 received timeout message");
                    Context.Saga1ReceivedTimeout = true;
                    return Task.FromResult(0);
                }
            }

            public class Saga2 : Saga<SagaData2>, IAmStartedByMessages<StartSagaMessage>, IHandleTimeouts<SagaTimeoutMessage>
            {
                public Context Context { get; set; }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData2> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.Id).ToSaga(s => s.CorrelationProperty);
                }

                public async Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Console.WriteLine("started saga2");
                    await RequestTimeout<SagaTimeoutMessage>(context, TimeSpan.FromSeconds(10));
                }

                public Task Timeout(SagaTimeoutMessage state, IMessageHandlerContext context)
                {
                    Console.WriteLine("Saga2 received timeout message");
                    Context.Saga2ReceivedTimeout = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class SagaData2 : ContainSagaData
        {
            public string CorrelationProperty { get; set; }
        }

        public class SagaData1 : ContainSagaData
        {
            public string CorrelationProperty { get; set; }
        }

        public class StartSagaMessage : ICommand
        {
            public string Id { get; set; }
        }

        public class SagaTimeoutMessage : IMessage
        {
        }
    }
}