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
            var context = new Context { Id = Guid.NewGuid() };

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) => bus.SendLocal(new StartSaga1 { ContextId = c.Id })))
                    .Done(c => (c.Saga1TimeoutFired && c.Saga2TimeoutFired) || c.SagaNotFound)
                    .Run(TimeSpan.FromSeconds(20));

            Assert.IsFalse(context.SagaNotFound);
            Assert.IsTrue(context.Saga1TimeoutFired);
            Assert.IsTrue(context.Saga2TimeoutFired);
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
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
                    if (message.ContextId != Context.Id) return;

                    RequestTimeout(TimeSpan.FromSeconds(5), new Saga1Timeout { ContextId = Context.Id });
                    RequestTimeout(new DateTime(2011, 10, 14, 23, 08, 0, DateTimeKind.Local), new Saga2Timeout { ContextId = Context.Id });
                }

                public void Timeout(Saga1Timeout state)
                {
                    MarkAsComplete();

                    if (state.ContextId != Context.Id) return;
                    Context.Saga1TimeoutFired = true;
                }

                public void Timeout(Saga2Timeout state)
                {
                    if (state.ContextId != Context.Id) return;
                    Context.Saga2TimeoutFired = true;
                }

                public class Saga1Data : ContainSagaData
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Saga1Data> mapper)
                {
                }
            }

            public class SagaNotFound : IHandleSagaNotFound
            {
                public Context Context { get; set; }

                public void Handle(object message)
                {
                    if (((dynamic)message).ContextId != Context.Id) return;

                    Context.SagaNotFound = true;
                }
            }

            public class CatchAllMessageHandler : IHandleMessages<object>
            {
                public void Handle(object message)
                {

                }
            }

            public class Foo : ISpecifyMessageHandlerOrdering
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
            public Guid ContextId { get; set; }
        }


        public class Saga1Timeout
        {
            public Guid ContextId { get; set; }
        }

        public class Saga2Timeout
        {
            public Guid ContextId { get; set; }
        }
    }
}