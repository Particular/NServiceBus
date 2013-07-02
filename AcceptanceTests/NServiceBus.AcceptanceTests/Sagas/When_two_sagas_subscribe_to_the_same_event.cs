
namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    // Repro for issue  https://github.com/NServiceBus/NServiceBus/issues/1277
    public class When_two_sagas_subscribe_to_the_same_event : NServiceBusAcceptanceTest
    {

        [Test]
        public void Should_invoke_all_handlers_on_all_sagas()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<EndpointThatHostsTwoSagas>(b => b.Given(bus => bus.SendLocal(new StartSaga2 { DataId = Guid.NewGuid() })))
                    .WithEndpoint<EndpointThatHandlesAMessageAndPublishesEvent>()
                    .Done(c => c.DidSaga1EventHandlerGetInvoked && c.DidSaga2EventHandlerGetInvoked)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c =>
                        {
                            Assert.True(c.DidSaga1EventHandlerGetInvoked && c.DidSaga2EventHandlerGetInvoked);
                        })
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool DidSaga1EventHandlerGetInvoked { get; set; }
            public bool DidSaga2EventHandlerGetInvoked { get; set; }
        }

        public class EndpointThatHandlesAMessageAndPublishesEvent : EndpointConfigurationBuilder
        {
            public EndpointThatHandlesAMessageAndPublishesEvent()
            {
                EndpointSetup<DefaultServer>();
            }

            class OpenGroupCommandHandler : IHandleMessages<OpenGroupCommand>
            {
                public IBus Bus { get; set; }

                public void Handle(OpenGroupCommand message)
                {
                    Console.WriteLine("Received OpenGroupCommand for DataId:{0} ... and publishing GroupPendingEvent", message.DataId);
                    Bus.Publish(new GroupPendingEvent { DataId = message.DataId });
                }
            }
        }

        public class EndpointThatHostsTwoSagas : EndpointConfigurationBuilder
        {
            public EndpointThatHostsTwoSagas()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<OpenGroupCommand>(typeof(EndpointThatHandlesAMessageAndPublishesEvent))
                    .AddMapping<GroupPendingEvent>(typeof(EndpointThatHandlesAMessageAndPublishesEvent));
            }

            public class Saga1 : Saga<Saga1.MySaga1Data>, IAmStartedByMessages<GroupPendingEvent>, IHandleMessages<CompleteSaga1Now>
            {
                public Context Context { get; set; }

                public void Handle(GroupPendingEvent message)
                {
                    Data.DataId = message.DataId;
                    Console.Out.WriteLine("Saga1 received GroupPendingEvent for DataId: {0}", message.DataId);
                    Context.DidSaga1EventHandlerGetInvoked = true;
                    Bus.SendLocal(new CompleteSaga1Start { DataId = message.DataId });
                }

                public void Handle(CompleteSaga1Now message)
                {
                    Console.Out.WriteLine("Saga1 received CompleteSaga1Now for DataId:{0} and MarkAsComplete", message.DataId);

                    MarkAsComplete();
                }

                public override void ConfigureHowToFindSaga()
                {
                    ConfigureMapping<GroupPendingEvent>(m => m.DataId).ToSaga(s => s.DataId);
                    ConfigureMapping<CompleteSaga1Now>(m => m.DataId).ToSaga(s => s.DataId);
                }

                public class MySaga1Data : ContainSagaData
                {
                    [Unique]
                    public Guid DataId { get; set; }
                }
            }

            public class Saga2 : Saga<Saga2.MySaga2Data>, IAmStartedByMessages<StartSaga2>, IHandleMessages<GroupPendingEvent>
            {
                public Context Context { get; set; }
         
                public void Handle(StartSaga2 message)
                {
                    var dataId = Guid.NewGuid();
                    Console.Out.WriteLine("Saga2 sending OpenGroupCommand for DataId: {0}", dataId);
                    Data.DataId = dataId;
                    Bus.Send(new OpenGroupCommand { DataId = dataId });
                }

                public void Handle(GroupPendingEvent message)
                {
                    Context.DidSaga2EventHandlerGetInvoked = true;
                    Console.Out.WriteLine("Saga2 received GroupPendingEvent for DataId: {0} and MarkAsComplete", message.DataId);
                    MarkAsComplete();
                }

                public override void ConfigureHowToFindSaga()
                {
                    ConfigureMapping<StartSaga2>(m => m.DataId).ToSaga(s => s.DataId);
                    ConfigureMapping<GroupPendingEvent>(m => m.DataId).ToSaga(s => s.DataId);
                }

                public class MySaga2Data : ContainSagaData
                {
                    [Unique]
                    public Guid DataId { get; set; }
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

        public class CompleteSaga1Start : ICommand
        {
            public Guid DataId { get; set; }
        }

        public class CompleteSaga1Now : ICommand
        {
            public Guid DataId { get; set; }
        }
    }
}
