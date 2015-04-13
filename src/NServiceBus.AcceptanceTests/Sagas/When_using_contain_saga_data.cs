namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Saga;
    using NUnit.Framework;

    // Repro for #SB-191
    public class When_using_contain_saga_data : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_handle_timeouts_properly()
        {
            var context = Scenario.Define<Context>()
                    .WithEndpoint<EndpointThatHostsASaga>(
                        b => b.Given(bus => bus.SendLocal(new StartSaga {DataId = Guid.NewGuid()})))
                    .Done(c => c.TimeoutReceived)
                    .Run();

            Assert.True(context.TimeoutReceived);
        }

        public class Context : ScenarioContext
        {
            public bool TimeoutReceived { get; set; }
        }

        public class EndpointThatHostsASaga : EndpointConfigurationBuilder
        {
            public EndpointThatHostsASaga()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MySaga : Saga<MySaga.MySagaData>,
                                        IAmStartedByMessages<StartSaga>,
                                        IHandleTimeouts<MySaga.TimeHasPassed>
            {
                public Context Context { get; set; }

                public void Handle(StartSaga message)
                {
                    Data.DataId = message.DataId;

                    RequestTimeout(TimeSpan.FromSeconds(5), new TimeHasPassed());
                }

                public void Timeout(TimeHasPassed state)
                {
                    MarkAsComplete();

                    Context.TimeoutReceived = true;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.DataId).ToSaga(s => s.DataId);
                }

                public class MySagaData : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }

                public class TimeHasPassed
                {
                }

            }
        }

        public class StartSaga : IMessage
        {
            public Guid DataId { get; set; }
        }
    }
}
