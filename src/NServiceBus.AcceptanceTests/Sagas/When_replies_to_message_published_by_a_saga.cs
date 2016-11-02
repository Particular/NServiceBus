﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using Routing;
    using ScenarioDescriptors;

    public class When_replies_to_message_published_by_a_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_reply_to_a_message_published_by_a_saga()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>
                (b => b.When(c => c.Subscribed, session => session.SendLocal(new StartSaga
                {
                    DataId = Guid.NewGuid()
                }))
                )
                .WithEndpoint<ReplyEndpoint>(b => b.When(async (session, context) =>
                {
                    await session.Subscribe<DidSomething>();
                    if (context.HasNativePubSubSupport)
                    {
                        context.Subscribed = true;
                    }
                }))
                .Done(c => c.DidSagaReplyMessageGetCorrelated)
                .Repeat(r => r.For(Transports.Default))
                .Should(c => Assert.True(c.DidSagaReplyMessageGetCorrelated))
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool DidSagaReplyMessageGetCorrelated { get; set; }
            public bool Subscribed { get; set; }
        }

        public class ReplyEndpoint : EndpointConfigurationBuilder
        {
            public ReplyEndpoint()
            {
                EndpointSetup<DefaultServer>(b => b.DisableFeature<AutoSubscribe>(), metadata => metadata.RegisterPublisherFor<DidSomething>(typeof(SagaEndpoint)));
            }

            class DidSomethingHandler : IHandleMessages<DidSomething>
            {
                public Task Handle(DidSomething message, IMessageHandlerContext context)
                {
                    return context.Reply(new DidSomethingResponse
                    {
                        ReceivedDataId = message.DataId
                    });
                }
            }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.EnableFeature<TimeoutManager>();
                    b.OnEndpointSubscribed<Context>((s, context) => { context.Subscribed = true; });
                });
            }

            public class ReplyToPubMsgSaga : Saga<ReplyToPubMsgSaga.ReplyToPubMsgSagaData>, IAmStartedByMessages<StartSaga>, IHandleMessages<DidSomethingResponse>
            {
                public Context Context { get; set; }

                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.DataId = message.DataId;
                    return context.Publish(new DidSomething
                    {
                        DataId = message.DataId
                    });
                }

                public Task Handle(DidSomethingResponse message, IMessageHandlerContext context)
                {
                    Context.DidSagaReplyMessageGetCorrelated = message.ReceivedDataId == Data.DataId;
                    MarkAsComplete();
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ReplyToPubMsgSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.DataId).ToSaga(s => s.DataId);
                }

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
}