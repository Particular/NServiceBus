namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

// Repro for issue  https://github.com/NServiceBus/NServiceBus/issues/1277
public class When_two_sagas_subscribe_to_the_same_event : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_invoke_all_handlers_on_all_sagas()
    {
        // exclude the brokers since c.Subscribed won't get set for them
        Requires.MessageDrivenPubSub();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>()
            .WithEndpoint<SagaEndpoint>(b =>
                b.When(c => c.Subscribed, session => session.SendLocal(new StartSaga2
                {
                    DataId = Guid.NewGuid()
                }))
            )
            .Run();

        Assert.That(context.DidSaga1EventHandlerGetInvoked && context.DidSaga2EventHandlerGetInvoked, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool Subscribed { get; set; }
        public bool DidSaga1EventHandlerGetInvoked { get; set; }
        public bool DidSaga2EventHandlerGetInvoked { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(DidSaga1EventHandlerGetInvoked, DidSaga2EventHandlerGetInvoked);
    }

    public class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() =>
            EndpointSetup<DefaultPublisher>(b =>
            {
                b.OnEndpointSubscribed<Context>((s, context) => { context.Subscribed = true; });
            }, metadata => metadata.RegisterSelfAsPublisherFor<GroupPendingEvent>(this));

        [Handler]
        public class OpenGroupCommandHandler : IHandleMessages<OpenGroupCommand>
        {
            public Task Handle(OpenGroupCommand message, IMessageHandlerContext context) =>
                context.Publish(new GroupPendingEvent
                {
                    DataId = message.DataId
                });
        }
    }

    public class SagaEndpoint : EndpointConfigurationBuilder
    {
        public SagaEndpoint() =>
            EndpointSetup<DefaultServer>(c =>
                {
                    c.ConfigureRouting().RouteToEndpoint(typeof(OpenGroupCommand), typeof(Publisher));
                },
                metadata => metadata.RegisterPublisherFor<GroupPendingEvent, Publisher>());

        [Saga]
        public class Saga1(Context testContext) : Saga<Saga1.MySaga1Data>,
            IAmStartedByMessages<GroupPendingEvent>,
            IHandleMessages<CompleteSaga1Now>
        {
            public Task Handle(GroupPendingEvent message, IMessageHandlerContext context) =>
                context.SendLocal(new CompleteSaga1Now
                {
                    DataId = message.DataId
                });

            public Task Handle(CompleteSaga1Now message, IMessageHandlerContext context)
            {
                testContext.DidSaga1EventHandlerGetInvoked = true;
                testContext.MaybeCompleted();

                MarkAsComplete();

                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySaga1Data> mapper) =>
                mapper.MapSaga(s => s.DataId)
                    .ToMessage<GroupPendingEvent>(m => m.DataId)
                    .ToMessage<CompleteSaga1Now>(m => m.DataId);

            public class MySaga1Data : ContainSagaData
            {
                public virtual Guid DataId { get; set; }
            }
        }

        [Saga]
        public class Saga2(Context testContext) : Saga<Saga2.MySaga2Data>,
            IAmStartedByMessages<StartSaga2>,
            IHandleMessages<GroupPendingEvent>
        {
            public Task Handle(StartSaga2 message, IMessageHandlerContext context) =>
                context.Send(new OpenGroupCommand
                {
                    DataId = Data.DataId
                });

            public Task Handle(GroupPendingEvent message, IMessageHandlerContext context)
            {
                testContext.DidSaga2EventHandlerGetInvoked = true;
                testContext.MaybeCompleted();
                MarkAsComplete();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySaga2Data> mapper) =>
                mapper.MapSaga(s => s.DataId)
                    .ToMessage<StartSaga2>(m => m.DataId)
                    .ToMessage<GroupPendingEvent>(m => m.DataId);

            public class MySaga2Data : ContainSagaData
            {
                public virtual Guid DataId { get; set; }
            }
        }
    }

    public class GroupPendingEvent : IEvent
    {
        public Guid DataId { get; set; }
    }

    public class OpenGroupCommand : ICommand
    {
        public Guid DataId { get; set; }
    }

    public class StartSaga2 : ICommand
    {
        public Guid DataId { get; set; }
    }

    public class CompleteSaga1Now : ICommand
    {
        public Guid DataId { get; set; }
    }
}