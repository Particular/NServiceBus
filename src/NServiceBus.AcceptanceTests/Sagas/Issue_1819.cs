namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;

    public class Issue_1819 : NServiceBusAcceptanceTest
    {
        [Test]
        public void Run()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new StartSaga1())))
                    .Done(c => (c.Saga1TimeoutFired && c.Saga2TimeoutFired) || c.SagaNotFound)
                    .Run(TimeSpan.FromSeconds(20));

            Assert.IsFalse(context.SagaNotFound);
            Assert.IsTrue(context.Saga1TimeoutFired);
            Assert.IsTrue(context.Saga2TimeoutFired);
        }

        public class Context : ScenarioContext
        {
            public bool Saga1TimeoutFired { get; set; }
            public bool Saga2TimeoutFired { get; set; }
            public bool SagaNotFound { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class Saga1 : Saga<Saga1.Saga1Data>, IAmStartedByMessages<StartSaga1>, IHandleTimeouts<Saga1Timeout>, IHandleTimeouts<Saga2Timeout>
            {
                public Context Context { get; set; }

                public void Handle(StartSaga1 message)
                {
                    RequestTimeout<Saga1Timeout>(TimeSpan.FromSeconds(5));
                    RequestTimeout<Saga2Timeout>(new DateTime(2011, 10, 14, 23, 08, 0, DateTimeKind.Local));
                }

                public void Timeout(Saga1Timeout state)
                {
                    MarkAsComplete();
                    Context.Saga1TimeoutFired = true;
                }

                public void Timeout(Saga2Timeout state)
                {
                    Context.Saga2TimeoutFired = true;
                }

                public class Saga1Data : ContainSagaData
                {
                }
            }

            public class SagaNotFound : IHandleSagaNotFound
            {
                public Context Context { get; set; }

                public void Handle(object message)
                {
                    Context.SagaNotFound = true;
                }
            }

            public class CatchAllMessageHandler : IHandleMessages<object>
            {
                public void Handle(object message)
                {

                }
            }

            public class Foo: ISpecifyMessageHandlerOrdering
            {
                public void SpecifyOrder(Order order)
                {
                    order.SpecifyFirst<CatchAllMessageHandler>();
                }
            }
        }

        [Serializable]
        public class StartSaga1 : ICommand
        {
        }


        public class Saga1Timeout
        {
        }

        public class Saga2Timeout
        {
        }
    }
}