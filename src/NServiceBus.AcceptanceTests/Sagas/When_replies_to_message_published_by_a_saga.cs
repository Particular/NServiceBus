namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using Features;
    using NServiceBus.AcceptanceTests.Routing;
    using NServiceBus.Config;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_replies_to_message_published_by_a_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_reply_to_a_message_published_by_a_saga()
        {
            Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>
                (b => b.When(c => c.Subscribed, bus => bus.SendLocal(new StartSaga
                {
                    DataId = Guid.NewGuid()
                }))
                )
                .WithEndpoint<ReplyEndpoint>(b => b.Given((bus, context) =>
                {
                    bus.Subscribe<DidSomething>();
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
                EndpointSetup<DefaultServer>(b => b.DisableFeature<AutoSubscribe>())
                    .AddMapping<DidSomething>(typeof(SagaEndpoint))
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    })
                    .WithConfig<SecondLevelRetriesConfig>(c =>
                    {
                        c.Enabled = false;
                    });
            }

            class DidSomethingHandler : IHandleMessages<DidSomething>
            {
                public IBus Bus { get; set; }

                public void Handle(DidSomething message)
                {
                    Bus.Reply(new DidSomethingResponse { ReceivedDataId = message.DataId });
                }
            }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) =>
                {
                    context.Subscribed = true;
                }));
            }

            public class ReplyToPubMsgSaga : Saga<ReplyToPubMsgSaga.ReplyToPubMsgSagaData>, IAmStartedByMessages<StartSaga>, IHandleMessages<DidSomethingResponse>
            {
                public Context Context { get; set; }

                public void Handle(StartSaga message)
                {
                    Data.DataId = message.DataId;
                    Bus.Publish(new DidSomething { DataId = message.DataId });
                }

                public void Handle(DidSomethingResponse message)
                {
                    Context.DidSagaReplyMessageGetCorrelated = message.ReceivedDataId == Data.DataId;
                    MarkAsComplete();
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
        
        [Serializable]
        public class StartSaga : ICommand
        {
            public Guid DataId { get; set; }
        }

        [Serializable]
        public class DidSomething : IEvent
        {
            public Guid DataId { get; set; }
        }

        [Serializable]
        public class DidSomethingResponse : IMessage
        {
            public Guid ReceivedDataId { get; set; }
        }
    }
}
