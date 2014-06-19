namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    //repro for issue: https://github.com/NServiceBus/NServiceBus/issues/1020
    public class When_a_saga_message_goes_through_the_slr : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_invoke_the_correct_handle_methods_on_the_saga()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<SagaEndpoint>(b => b.Given(bus => bus.SendLocal(new StartSagaMessage { SomeId = Guid.NewGuid() })))
                    .AllowExceptions()
                    .Done(c => c.SecondMessageProcessed)
                    .Repeat(r => r.For(Transports.Default))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool SecondMessageProcessed { get; set; }


            public int NumberOfTimesInvoked { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartSagaMessage>,IHandleMessages<SecondSagaMessage>
            {
                public Context Context { get; set; }
                public void Handle(StartSagaMessage message)
                {
                    Data.SomeId = message.SomeId;

                    Bus.SendLocal(new SecondSagaMessage
                        {
                            SomeId = Data.SomeId
                        });
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s=>s.SomeId);
                    mapper.ConfigureMapping<SecondSagaMessage>(m => m.SomeId)
                      .ToSaga(s => s.SomeId);
                }

                public void Handle(SecondSagaMessage message)
                {
                    Context.NumberOfTimesInvoked++;
                    var shouldFail = Context.NumberOfTimesInvoked < 2; //1 FLR and 1 SLR

                    if(shouldFail)
                        throw new Exception("Simulated exception");

                    Context.SecondMessageProcessed = true;
                }

            }

            public class TestSagaData : IContainSagaData
            {
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
                
                [Unique]
                public virtual Guid SomeId { get; set; }
            }
        }

        [Serializable]
        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
        public class SecondSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
        
        public class SomeTimeout
        {
        }
    }


}