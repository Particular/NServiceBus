namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Features;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    public class When_timeout_hit_not_found_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_fire_notfound_for_tm()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(bus => bus.SendLocalAsync(new StartSaga())))
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

            public class TimeoutHitsNotFoundSaga : Saga<TimeoutHitsNotFoundSaga.TimeoutHitsNotFoundSagaData>,
                IAmStartedByMessages<StartSaga>, 
                IHandleSagaNotFound,
                IHandleTimeouts<TimeoutHitsNotFoundSaga.MyTimeout>,
                IHandleMessages<SomeOtherMessage>
            {
                public Context Context { get; set; }

                public async Task Handle(StartSaga message)
                {
                    Data.DataId = message.DataId;

                    //this will cause the message to be delivered right away
                    await RequestTimeoutAsync<MyTimeout>(TimeSpan.Zero);
                    await Bus.SendLocalAsync(new SomeOtherMessage { DataId = Guid.NewGuid() });

                    MarkAsComplete();
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TimeoutHitsNotFoundSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.DataId).ToSaga(s => s.DataId);
                    mapper.ConfigureMapping<SomeOtherMessage>(m => m.DataId).ToSaga(s => s.DataId);
                }

                public class TimeoutHitsNotFoundSagaData : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }

                public class MyTimeout { }

                public Task Handle(object message)
                {
                    if (message is SomeOtherMessage)
                    {
                        Context.NotFoundHandlerCalledForRegularMessage = true;
                    }


                    if (message is MyTimeout)
                    {
                        Context.NotFoundHandlerCalledForTimeout = true;
                    }
                    return Task.FromResult(0);
                }

                public Task Handle(SomeOtherMessage message)
                {
                    return Task.FromResult(0);
                }

                public Task Timeout(MyTimeout state)
                {
                    return Task.FromResult(0);
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
