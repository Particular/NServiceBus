namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NUnit.Framework;

public class When_replying_to_saga_event : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_correlate_reply_to_publishing_saga_instance()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<SagaEndpoint>(b => b
                .When(c => c.Subscribed, session => session.SendLocal(new StartSaga
                {
                    DataId = Guid.NewGuid()
                }))
            )
            .WithEndpoint<ReplyEndpoint>(b => b
                .When(async (session, c) =>
                {
                    await session.Subscribe<DidSomething>();
                    if (c.HasNativePubSubSupport)
                    {
                        c.Subscribed = true;
                    }
                }))
            .Run();

        Assert.That(context.CorrelatedResponseReceived, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool CorrelatedResponseReceived { get; set; }
        public bool Subscribed { get; set; }
    }

    public class ReplyEndpoint : EndpointConfigurationBuilder
    {
        public ReplyEndpoint() => EndpointSetup<DefaultServer>(b => b.DisableFeature<AutoSubscribe>(), metadata => metadata.RegisterPublisherFor<DidSomething, SagaEndpoint>());

        [Handler]
        public class DidSomethingHandler : IHandleMessages<DidSomething>
        {
            public Task Handle(DidSomething message, IMessageHandlerContext context) =>
                context.Reply(new DidSomethingResponse
                {
                    ReceivedDataId = message.DataId
                });
        }
    }

    public class SagaEndpoint : EndpointConfigurationBuilder
    {
        public SagaEndpoint() =>
            EndpointSetup<DefaultPublisher>(b =>
            {
                b.OnEndpointSubscribed<Context>((s, context) => { context.Subscribed = true; });
            }, metadata => metadata.RegisterSelfAsPublisherFor<DidSomething>(this));

        [Saga]
        public class ReplyToPubMsgSaga(Context testContext) : Saga<ReplyToPubMsgSaga.ReplyToPubMsgSagaData>,
            IAmStartedByMessages<StartSaga>, IHandleMessages<DidSomethingResponse>
        {
            public Task Handle(StartSaga message, IMessageHandlerContext context) =>
                context.Publish(new DidSomething
                {
                    DataId = message.DataId
                });

            public Task Handle(DidSomethingResponse message, IMessageHandlerContext context)
            {
                testContext.CorrelatedResponseReceived = message.ReceivedDataId == Data.DataId;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ReplyToPubMsgSagaData> mapper) =>
                mapper.MapSaga(s => s.DataId)
                    .ToMessage<StartSaga>(m => m.DataId);

            public class ReplyToPubMsgSagaData : ContainSagaData
            {
                public virtual Guid DataId { get; set; }
            }
        }
    }

    public class StartSaga : ICommand
    {
        public Guid DataId { get; set; }
    }

    public class DidSomething : IEvent
    {
        public Guid DataId { get; set; }
    }

    public class DidSomethingResponse : IMessage
    {
        public Guid ReceivedDataId { get; set; }
    }
}