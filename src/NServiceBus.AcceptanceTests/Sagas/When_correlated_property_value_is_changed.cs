namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using EndpointTemplates;
    using NUnit.Framework;

    [TestFixture]
    public class When_correlated_property_value_is_changed : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw()
        {
            var exception = Assert.ThrowsAsync<MessageFailedException>(async () =>
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(
                        b => b.When(session => session.SendLocal(new StartSaga
                        {
                            DataId = Guid.NewGuid()
                        })))
                    .Done(c => c.ModifiedCorrelationProperty)
                    .Run());

            Assert.AreEqual(1, exception.ScenarioContext.FailedMessages.Count);
            StringAssert.Contains(
                "Changing the value of correlated properties at runtime is currently not supported",
                exception.FailedMessage.Exception.Message);
        }

        public class Context : ScenarioContext
        {
            public bool ModifiedCorrelationProperty { get; set; }
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
                public CorrIdChangedSaga(Context context)
                {
                    testContext = context;
                }

                public Task Handle(StartSaga message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    Data.DataId = Guid.NewGuid();
                    testContext.ModifiedCorrelationProperty = true;
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

                Context testContext;
            }
        }

        public class StartSaga : ICommand
        {
            public Guid DataId { get; set; }
        }
    }
}