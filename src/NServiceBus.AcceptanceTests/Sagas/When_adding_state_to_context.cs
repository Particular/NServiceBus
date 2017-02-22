namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using NServiceBus;
    using NServiceBus.Pipeline;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence;

    [TestFixture]
    public class When_adding_state_to_context : NServiceBusAcceptanceTest
    {
        [Test, Ignore("Not sure what to do here, since the start message has no corr prop we can't generate the SagaId, perhaps custom finder isn't supported for the devstorage?")]
        public async Task Should_make_state_available_to_finder_context()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b => b
                    .When(session => session.SendLocal(new StartSagaMessage())))
                .Done(c => c.FinderUsed)
                .Run();

            Assert.True(context.FinderUsed);
            Assert.AreEqual("SomeData", context.ContextBag.Get<SagaEndpoint.BehaviorWhichAddsThingsToTheContext.State>().SomeData);
        }

        public class Context : ScenarioContext
        {
            public bool FinderUsed { get; set; }
            public ReadOnlyContextBag ContextBag { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Pipeline.Register(new BehaviorWhichAddsThingsToTheContext(), "adds some data to the context");
                });
            }

            class CustomFinder : IFindSagas<TestSaga07.SagaData07>.Using<StartSagaMessage>
            {
                public Context Context { get; set; }

                public Task<TestSaga07.SagaData07> FindBy(StartSagaMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context)
                {
                    Context.ContextBag = context;
                    Context.FinderUsed = true;
                    return Task.FromResult(default(TestSaga07.SagaData07));
                }
            }

            public class TestSaga07 : Saga<TestSaga07.SagaData07>, IAmStartedByMessages<StartSagaMessage>
            {
                public Context Context { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData07> mapper)
                {
                    // custom finder used
                }

                public class SagaData07 : ContainSagaData
                {
                }
            }

            public class BehaviorWhichAddsThingsToTheContext : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
            {
                public Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
                {
                    context.Extensions.Set(new State
                    {
                        SomeData = "SomeData"
                    });

                    return next(context);
                }

                public class State
                {
                    public string SomeData { get; set; }
                }
            }
        }

        public class StartSagaMessage : IMessage
        {
        }
    }
}