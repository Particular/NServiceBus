namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests.Routing;
    using NServiceBus.Features;
    using NUnit.Framework;
    using ScenarioDescriptors;

    // Repro for issue  https://github.com/NServiceBus/NServiceBus/issues/1277
    public class When_two_sagas_subscribe_to_the_same_event : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_invoke_all_handlers_on_all_sagas()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<Publisher>(b => b.When((bus, context) =>
                    {
                        if (context.HasNativePubSubSupport)
                        {
                            context.Subscribed = true;
                            context.AddTrace("EndpointThatHandlesAMessageAndPublishesEvent is now subscribed (at least we have asked the broker to be subscribed)");
                        }
                        return Task.FromResult(0);
                    }))
                    .WithEndpoint<SagaEndpoint>(b =>
                        b.When(c => c.Subscribed, bus => bus.SendLocal(new StartSaga2
                        {
                            DataId = Guid.NewGuid()
                        }))
                     )
                    .Done(c => c.DidSaga1EventHandlerGetInvoked && c.DidSaga2EventHandlerGetInvoked)
                    .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>()) // exclude the brokers since c.Subscribed won't get set for them
                    .Should(c => Assert.True(c.DidSaga1EventHandlerGetInvoked && c.DidSaga2EventHandlerGetInvoked))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool Subscribed { get; set; }
            public bool DidSaga1EventHandlerGetInvoked { get; set; }
            public bool DidSaga2EventHandlerGetInvoked { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.EnableFeature<TimeoutManager>();
                    b.OnEndpointSubscribed<Context>((s, context) => { context.Subscribed = true; });
                });
            }

            class OpenGroupCommandHandler : IHandleMessages<OpenGroupCommand>
            {
                public Task Handle(OpenGroupCommand message, IMessageHandlerContext context)
                {
                    Console.WriteLine("Received OpenGroupCommand for RunId:{0} ... and publishing GroupPendingEvent", message.DataId);
                    return context.Publish(new GroupPendingEvent { DataId = message.DataId });
                }
            }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.EnableFeature<TimeoutManager>())
                    .AddMapping<OpenGroupCommand>(typeof(Publisher))
                    .AddMapping<GroupPendingEvent>(typeof(Publisher));
            }

            public class Saga1 : Saga<Saga1.MySaga1Data>, 
                IAmStartedByMessages<GroupPendingEvent>, 
                IHandleMessages<CompleteSaga1Now>
            {
                public Context TestContext { get; set; }

                public Task Handle(GroupPendingEvent message, IMessageHandlerContext context)
                {
                    Console.Out.WriteLine("Saga1 received GroupPendingEvent for RunId: {0}", message.DataId);
                    return context.SendLocal(new CompleteSaga1Now { DataId = message.DataId });
                }

                public Task Handle(CompleteSaga1Now message, IMessageHandlerContext context)
                {
                    Console.Out.WriteLine("Saga1 received CompleteSaga1Now for RunId:{0} and MarkAsComplete", message.DataId);
                    TestContext.DidSaga1EventHandlerGetInvoked = true;

                    MarkAsComplete();

                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySaga1Data> mapper)
                {
                    mapper.ConfigureMapping<GroupPendingEvent>(m => m.DataId).ToSaga(s => s.DataId);
                    mapper.ConfigureMapping<CompleteSaga1Now>(m => m.DataId).ToSaga(s => s.DataId);
                }

                public class MySaga1Data : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }
            }

            public class Saga2 : Saga<Saga2.MySaga2Data>, 
                IAmStartedByMessages<StartSaga2>, 
                IHandleMessages<GroupPendingEvent>
            {
                public Context TestContext { get; set; }

                public Task Handle(StartSaga2 message, IMessageHandlerContext context)
                {
                    Console.Out.WriteLine("Saga2 sending OpenGroupCommand for RunId: {0}", Data.DataId);
                    return context.Send(new OpenGroupCommand { DataId = Data.DataId });
                }

                public Task Handle(GroupPendingEvent message, IMessageHandlerContext context)
                {
                    TestContext.DidSaga2EventHandlerGetInvoked = true;
                    Console.Out.WriteLine("Saga2 received GroupPendingEvent for RunId: {0} and MarkAsComplete", message.DataId);
                    MarkAsComplete();
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySaga2Data> mapper)
                {
                    mapper.ConfigureMapping<StartSaga2>(m => m.DataId).ToSaga(s => s.DataId);
                    mapper.ConfigureMapping<GroupPendingEvent>(m => m.DataId).ToSaga(s => s.DataId);
                }

                public class MySaga2Data : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }
            }
        }

        [Serializable]
        public class GroupPendingEvent : IEvent
        {
            public Guid DataId { get; set; }
        }

        public class OpenGroupCommand : ICommand
        {
            public Guid DataId { get; set; }
        }

        [Serializable]
        public class StartSaga2 : ICommand
        {
            public Guid DataId { get; set; }
        }

        public class CompleteSaga1Now : ICommand
        {
            public Guid DataId { get; set; }
        }
    }
}
