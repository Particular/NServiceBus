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
                .WithEndpoint<Endpoint>(b => b.When(bus => bus.SendLocal(new StartSaga { DataId = Guid.NewGuid() })))
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
                public Context TestContext { get; set; }

                public async Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.DataId = message.DataId;

                    //this will cause the message to be delivered right away
                    await RequestTimeout<MyTimeout>(context, TimeSpan.Zero);
                    await context.SendLocal(new SomeOtherMessage { DataId = Guid.NewGuid() });

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

                public Task Handle(object message, IMessageProcessingContext context)
                {
                    if (message is SomeOtherMessage)
                    {
                        TestContext.NotFoundHandlerCalledForRegularMessage = true;
                    }


                    if (message is MyTimeout)
                    {
                        TestContext.NotFoundHandlerCalledForTimeout = true;
                    }
                    return Task.FromResult(0);
                }

                public Task Handle(SomeOtherMessage message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                public Task Timeout(MyTimeout state, IMessageHandlerContext context)
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
