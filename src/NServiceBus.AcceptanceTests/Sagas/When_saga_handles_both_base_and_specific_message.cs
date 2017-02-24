namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_saga_handles_both_base_and_specific_message : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_invoke_handle_on_the_same_instance()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<PolymorphicSagaEndpoint>(b => b
                    .When(session => session.SendLocal(new StartSagaMessage
                    {
                        SomeId = Guid.NewGuid()
                    })))
                .Done(c => c.BaseId != Guid.Empty && c.SpecificId != Guid.Empty)
                .Run();

            Assert.AreEqual(context.BaseId, context.SpecificId, "The same saga instance should be used");
        }

        public class Context : ScenarioContext
        {
            public Guid BaseId { get; set; }
            public Guid SpecificId { get; set; }
        }

        public class PolymorphicSagaEndpoint : EndpointConfigurationBuilder
        {
            public PolymorphicSagaEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class PolymorphicSaga : Saga<PolymorphicSagaData>,
                IAmStartedByMessages<StartSagaMessageBase>,
                IAmStartedByMessages<StartSagaMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    TestContext.SpecificId = Data.Id;

                    return Task.FromResult(0);
                }

                public Task Handle(StartSagaMessageBase message, IMessageHandlerContext context)
                {
                    TestContext.BaseId = Data.Id;

                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<PolymorphicSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessageBase>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }
            }

            public class PolymorphicSagaData : ContainSagaData
            {
                public virtual Guid SomeId { get; set; }
            }
        }

        public class StartSagaMessage : StartSagaMessageBase
        {
        }

        public class StartSagaMessageBase : IMessage
        {
            public Guid SomeId { get; set; }
        }
    }
}