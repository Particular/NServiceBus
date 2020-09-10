namespace NServiceBus.AcceptanceTests.Sagas
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    class When_correlating_special_chars : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Saga_persistence_and_correlation_should_work()
        {
            const string propertyValue = "ʕノ•ᴥ•ʔノ ︵ ┻━┻";

            var context = await Scenario.Define<Context>()
                .WithEndpoint<SpecialCharacterSagaEndpoint>(e => e
                    .When(s => s.SendLocal(new MessageWithSpecialPropertyValues
                    {
                        SpecialCharacterValues = propertyValue
                    })))
                .Done(c => c.RehydratedValueForCorrelatedHandler != null)
                .Run();

            Assert.AreEqual(propertyValue, context.RehydratedValueForCorrelatedHandler);
        }

        public class Context : ScenarioContext
        {
            public string RehydratedValueForCorrelatedHandler { get; set; }
        }

        public class SpecialCharacterSagaEndpoint : EndpointConfigurationBuilder
        {
            public SpecialCharacterSagaEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class SagaDataSpecialValues : ContainSagaData
            {
                public virtual string SpecialCharacterValues { get; set; }
            }

            public class SagaSpecialValues :
                Saga<SagaDataSpecialValues>,
                IAmStartedByMessages<MessageWithSpecialPropertyValues>,
                IHandleMessages<FollowupMessageWithSpecialPropertyValues>
            {
                public SagaSpecialValues(Context testContext)
                {
                    this.testContext = testContext;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaDataSpecialValues> mapper)
                {
                    mapper.ConfigureMapping<MessageWithSpecialPropertyValues>(m => m.SpecialCharacterValues).ToSaga(s => s.SpecialCharacterValues);
                    mapper.ConfigureMapping<FollowupMessageWithSpecialPropertyValues>(m => m.SpecialCharacterValues).ToSaga(s => s.SpecialCharacterValues);
                }

                public Task Handle(MessageWithSpecialPropertyValues message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    return context.SendLocal(new FollowupMessageWithSpecialPropertyValues
                    {
                        SpecialCharacterValues = message.SpecialCharacterValues
                    });
                }

                public Task Handle(FollowupMessageWithSpecialPropertyValues message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.RehydratedValueForCorrelatedHandler = Data.SpecialCharacterValues;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MessageWithSpecialPropertyValues : ICommand
        {
            public string SpecialCharacterValues { get; set; }
        }

        public class FollowupMessageWithSpecialPropertyValues : ICommand
        {
            public string SpecialCharacterValues { get; set; }
        }
    }
}
