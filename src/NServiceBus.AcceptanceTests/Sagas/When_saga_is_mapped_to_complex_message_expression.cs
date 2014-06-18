namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_saga_is_mapped_to_complex_message_expression : NServiceBusAcceptanceTest
    {

        [Test]
        public void Should_hydrate_and_invoke_the_existing_instance()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<SagaEndpoint>(b => b.Given(bus =>
                        {
                            bus.SendLocal(new StartSagaMessage { Key = "Part1_Part2"});
                            bus.SendLocal(new OtherMessage { Part1 = "Part1", Part2 = "Part2" });                                    
                        }))
                    .Done(c => c.SecondMessageReceived)
                    .Repeat(r => r.For(Persistence.Default))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool SecondMessageReceived { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(c=>c.Transactions(t=>t.Advanced(a => a.DoNotWrapHandlersExecutionInATransactionScope())));
            }

            public class TestSaga : Saga<TestSagaData>,
                IAmStartedByMessages<StartSagaMessage>, IHandleMessages<OtherMessage>
            {
                public Context Context { get; set; }
                public void Handle(StartSagaMessage message)
                {
                    Data.Key = message.Key;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
                {
                    mapper.ConfigureMapping<OtherMessage>(m => m.Part1 + "_"+m.Part2)
                        .ToSaga(s => s.Key);
                }

                public void Handle(OtherMessage message)
                {
                    Context.SecondMessageReceived = true;
                }
            }

            public class TestSagaData : IContainSagaData
            {
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
                public virtual string Key { get; set; }
            }
        }

        [Serializable]
        public class StartSagaMessage : ICommand
        {
            public string Key { get; set; }
        }
        [Serializable]
        public class OtherMessage : ICommand
        {
            public string Part2 { get; set; }
            public string Part1 { get; set; }
        }
    }
}