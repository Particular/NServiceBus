namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Features;
    using NUnit.Framework;
    using Saga;

    public class When_timeout_hit_not_found_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_fire_notfound_for_tm()
        {
            var context = Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new StartSaga())))
                .Done(c => c.NotFoundHandlerCalledForRegularMessage)
                .Run();


            Assert.False(context.NotFoundHandlerCalledForTimeout);
        }

        public class Context : ScenarioContext
        {
            public bool NotFoundHandlerCalledForRegularMessage { get; set; }
            public bool NotFoundHandlerCalledForTimeout { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }

            public class MySaga : Saga<MySaga.MySagaData>,
                IAmStartedByMessages<StartSaga>, IHandleSagaNotFound,
                IHandleTimeouts<MySaga.MyTimeout>,
                IHandleMessages<SomeOtherMessage>
            {
                public Context Context { get; set; }

                public void Handle(StartSaga message)
                {
                    Data.DataId = message.DataId;

                    //this will cause the message to be delivered right away
                    RequestTimeout<MyTimeout>(TimeSpan.Zero);
                    Bus.SendLocal(new SomeOtherMessage { DataId = Guid.NewGuid() });

                    MarkAsComplete();
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.DataId).ToSaga(s => s.DataId);
                    mapper.ConfigureMapping<SomeOtherMessage>(m => m.DataId).ToSaga(s => s.DataId);
                }

                public class MySagaData : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }

                public class MyTimeout { }

                public void Handle(object message)
                {
                    if (message is SomeOtherMessage)
                    {
                        Context.NotFoundHandlerCalledForRegularMessage = true;
                    }


                    if (message is MyTimeout)
                    {
                        Context.NotFoundHandlerCalledForTimeout = true;
                    }

                }

                public void Handle(SomeOtherMessage message)
                {
                }

                public void Timeout(MyTimeout state)
                {
                }
            }
        }

        public class StartSaga : IMessage
        {
            public Guid DataId { get; set; }
        }

        public class SomeOtherMessage : IMessage
        {
            public Guid DataId { get; set; }
        }
    }
}
