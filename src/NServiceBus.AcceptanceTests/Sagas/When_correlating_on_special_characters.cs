﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    class When_correlating_on_special_characters : NServiceBusAcceptanceTest
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

            public class SagaDataWithSpecialPropertyValues : ContainSagaData
            {
                public virtual string SpecialCharacterValues { get; set; }
            }

            public class SagaHandlingSpecialPropertyValues : 
                Saga<SagaDataWithSpecialPropertyValues>, 
                IAmStartedByMessages<MessageWithSpecialPropertyValues>,
                IHandleMessages<FollowupMessageWithSpecialPropertyValues>
            {
                Context testContext;

                public SagaHandlingSpecialPropertyValues(Context testContext)
                {
                    this.testContext = testContext;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaDataWithSpecialPropertyValues> mapper)
                {
                    mapper.ConfigureMapping<MessageWithSpecialPropertyValues>(m => m.SpecialCharacterValues).ToSaga(s => s.SpecialCharacterValues);
                    mapper.ConfigureMapping<FollowupMessageWithSpecialPropertyValues>(m => m.SpecialCharacterValues).ToSaga(s => s.SpecialCharacterValues);
                }

                public Task Handle(MessageWithSpecialPropertyValues message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new FollowupMessageWithSpecialPropertyValues
                    {
                        SpecialCharacterValues = message.SpecialCharacterValues
                    });
                }

                public Task Handle(FollowupMessageWithSpecialPropertyValues message, IMessageHandlerContext context)
                {
                    testContext.RehydratedValueForCorrelatedHandler = Data.SpecialCharacterValues;
                    return Task.FromResult(0);
                }
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
