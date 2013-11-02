﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    // Repro for #SB-191
    public class When_using_containsagadata : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_handle_timeouts_properly_when_using_NHibernate()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<EndpointThatHostsASaga>(
                        b => b.Given(bus => bus.SendLocal(new StartSaga {DataId = Guid.NewGuid()})))
                    .Done(c => c.DidAllSagaInstancesReceiveTimeouts)
                    .Repeat(r => r.For(Transports.SqlServer).For(SagaPersisters.NHibernate))
                    .Should(c =>
                        {
                            Assert.True(c.DidAllSagaInstancesReceiveTimeouts);
                        })
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

            public class SagaThatUsesNHibernatePersistence : Saga<SagaThatUsesNHibernatePersistence.MySagaData>,
                                                             IAmStartedByMessages<StartSaga>,
                                                             IHandleTimeouts<SagaThatUsesNHibernatePersistence.TimeHasPassed>
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

                public override void ConfigureHowToFindSaga()
                {
                    ConfigureMapping<StartSaga>(m => m.DataId).ToSaga(s => s.DataId);
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
