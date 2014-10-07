namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_sending_from_a_saga_timeout : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_match_different_saga()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new StartSaga1())))
                    .Done(c => c.DidSaga2ReceiveMessage)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.True(c.DidSaga2ReceiveMessage))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool DidSaga2ReceiveMessage { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {

            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class Saga1 : Saga<Saga1.Saga1Data>, IAmStartedByMessages<StartSaga1>, IHandleTimeouts<Saga1Timeout>
            {
                public Context Context { get; set; }

                public void Handle(StartSaga1 message)
                {
                    RequestTimeout(TimeSpan.FromSeconds(1), new Saga1Timeout());
                }

                public void Timeout(Saga1Timeout state)
                {
                    Bus.SendLocal(new StartSaga2());
                    MarkAsComplete();
                }
                public class Saga1Data : ContainSagaData
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Saga1Data> mapper)
                {
                }
            }

            public class Saga2 : Saga<Saga2.Saga2Data>, IAmStartedByMessages<StartSaga2>
            {
                public Context Context { get; set; }

                public void Handle(StartSaga2 message)
                {
                    Context.DidSaga2ReceiveMessage = true;
                }

                public class Saga2Data : ContainSagaData
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Saga2Data> mapper)
                {
                }
            }

        }


        [Serializable]
        public class StartSaga1 : ICommand
        {
        }

        [Serializable]
        public class StartSaga2 : ICommand
        {
        }

        public class Saga1Timeout : IMessage
        {
        }
    }
}