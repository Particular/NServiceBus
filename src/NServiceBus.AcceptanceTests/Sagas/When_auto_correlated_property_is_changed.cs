namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    [TestFixture]
    public class When_auto_correlated_property_is_changed : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_throw()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(
                    b => b.When(bus => bus.SendLocalAsync(new StartSaga
                    {
                        DataId = Guid.NewGuid()
                    })))
                .AllowExceptions()
                .Done(c => c.Exceptions.Any())
                .Run();

            Assert.True(context.Exceptions.Any(ex => ex.Message.Contains("Changing the value of correlated properties at runtime is currently not supported")));
        }

        public class Context : ScenarioContext
        {
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
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
                    mapper.ConfigureMapping<StartSaga>(m => m.DataId).ToSaga(s => s.DataId);
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