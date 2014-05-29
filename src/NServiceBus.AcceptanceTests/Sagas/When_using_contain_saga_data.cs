namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    // Repro for #SB-191
    public class When_using_contain_saga_data : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_handle_timeouts_properly()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<EndpointThatHostsASaga>(
                        b => b.Given(bus => bus.SendLocal(new StartSaga {DataId = Guid.NewGuid()})))
                    .Done(c => c.DidAllSagaInstancesReceiveTimeouts)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.True(c.DidAllSagaInstancesReceiveTimeouts))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool DidAllSagaInstancesReceiveTimeouts { get; set; }
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

                    Context.DidAllSagaInstancesReceiveTimeouts = true;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.DataId).ToSaga(s => s.DataId);
                }

                public class MySagaData : ContainSagaData
                {
                    [Unique]
                    public virtual Guid DataId { get; set; }
                }

                public class TimeHasPassed
                {
                }

            }
        }

        [Serializable]
        public class StartSaga : ICommand
        {
            public Guid DataId { get; set; }
        }
    }
}
