namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    [TestFixture]
    public class When_auto_correlated_property_is_changed : NServiceBusAcceptanceTest
    {
        [Test]
        public async void Should_throw()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b =>
                {
                    b.When(bus => bus.SendLocal(new StartSaga
                    {
                        DataId = Guid.NewGuid()
                    }));
                })
                .Done(c => c.Exception != null)
                .Run();

            var exception = context.Exception;
            StringAssert.Contains(
                "Changing the value of correlated properties at runtime is currently not supported",
                exception.Message);
        }

        public class Context : ScenarioContext
        {
            public Exception Exception { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(configure =>
                {
                    configure.Faults().SetFaultNotification(message =>
                    {
                        var testcontext = (Context)ScenarioContext;
                        testcontext.Exception = message.Exception;
                        return Task.FromResult(0);
                    });
                    configure.DisableFeature<FirstLevelRetries>();
                    configure.DisableFeature<SecondLevelRetries>();
                });
            }

            public class CorrIdChangedSaga : Saga<CorrIdChangedSaga.CorrIdChangedSagaData>,
                IAmStartedByMessages<StartSaga>
            {
                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.DataId = Guid.NewGuid();
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CorrIdChangedSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.DataId)
                        .ToSaga(s => s.DataId);
                }

                public class CorrIdChangedSagaData : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }
            }
        }

        public class StartSaga : ICommand
        {
            public Guid DataId { get; set; }
        }
    }
}