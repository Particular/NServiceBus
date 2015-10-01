namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    //repro for issue: https://github.com/NServiceBus/NServiceBus/issues/1020
    public class When_a_saga_message_goes_through_the_slr : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_invoke_the_correct_handle_methods_on_the_saga()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<SagaMsgThruSlrEndpt>(b => b.When(bus => bus.SendLocalAsync(new StartSagaMessage { SomeId = Guid.NewGuid() })))
                    .AllowSimulatedExceptions()
                    .Done(c => c.SecondMessageProcessed)
                    .Repeat(r => r.For(Transports.Default))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool SecondMessageProcessed { get; set; }


            public int NumberOfTimesInvoked { get; set; }
        }

        public class SagaMsgThruSlrEndpt : EndpointConfigurationBuilder
        {
            public SagaMsgThruSlrEndpt()
            {
                EndpointSetup<DefaultServer>();
            }

            public class TestSaga09 : Saga<TestSagaData09>, IAmStartedByMessages<StartSagaMessage>,IHandleMessages<SecondSagaMessage>
            {
                public Context Context { get; set; }
                public Task Handle(StartSagaMessage message)
                {
                    Data.SomeId = message.SomeId;

                    return Bus.SendLocalAsync(new SecondSagaMessage
                        {
                            SomeId = Data.SomeId
                        });
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData09> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s=>s.SomeId);
                    mapper.ConfigureMapping<SecondSagaMessage>(m => m.SomeId)
                      .ToSaga(s => s.SomeId);
                }

                public Task Handle(SecondSagaMessage message)
                {
                    Context.NumberOfTimesInvoked++;
                    var shouldFail = Context.NumberOfTimesInvoked < 2; //1 FLR and 1 SLR

                    if(shouldFail)
                        throw new SimulatedException();

                    Context.SecondMessageProcessed = true;

                    return Task.FromResult(0);
                }
            }

            public class TestSagaData09 : IContainSagaData
            {
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
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