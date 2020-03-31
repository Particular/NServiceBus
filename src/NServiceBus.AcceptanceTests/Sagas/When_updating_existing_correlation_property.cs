namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_updating_existing_correlation_property : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_blow_up()
        {
            var exception = Assert.ThrowsAsync<MessageFailedException>(async () =>
                await Scenario.Define<Context>()
                    .WithEndpoint<ChangePropertyEndpoint>(b => b.When(session => session.SendLocal(new StartSagaMessage
                    {
                        SomeId = Guid.NewGuid()
                    })))
                    .Done(c => c.FailedMessages.Any())
                    .Run());

            Assert.IsTrue(((Context)exception.ScenarioContext).ModifiedCorrelationProperty);
            Assert.AreEqual(1, exception.ScenarioContext.FailedMessages.Count);
            StringAssert.Contains(
                "Changing the value of correlated properties at runtime is currently not supported",
                exception.FailedMessage.Exception.Message);
        }

        public class Context : ScenarioContext
        {
            public bool ModifiedCorrelationProperty { get; set; }
        }

        public class ChangePropertyEndpoint : EndpointConfigurationBuilder
        {
            public ChangePropertyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class ChangeCorrPropertySaga : Saga<ChangeCorrPropertySagaData>, IAmStartedByMessages<StartSagaMessage>
            {
                public Context TestContext { get; set; }

                public ChangeCorrPropertySaga(Context testContext)
                {
                    TestContext = testContext;
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    if (message.SecondMessage)
                    {
                        Data.SomeId = Guid.NewGuid(); //this is not allowed
                        TestContext.ModifiedCorrelationProperty = true;
                        return Task.FromResult(0);
                    }

                    return context.SendLocal(new StartSagaMessage
                    {
                        SecondMessage = true,
                        SomeId = Data.SomeId
                    });
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ChangeCorrPropertySagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }
            }

            public class ChangeCorrPropertySagaData : IContainSagaData
            {
                public virtual Guid SomeId { get; set; }
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
            public bool SecondMessage { get; set; }
        }
    }
}