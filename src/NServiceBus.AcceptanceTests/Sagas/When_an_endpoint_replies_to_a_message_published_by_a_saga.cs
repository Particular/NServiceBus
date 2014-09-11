namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using Config;
    using EndpointTemplates;
    using AcceptanceTesting;
    using Features;
    using NUnit.Framework;
    using PubSub;
    using Saga;
    using ScenarioDescriptors;

    public class When_an_endpoint_replies_to_a_message_published_by_a_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_be_able_to_reply_to_a_message_published_by_a_saga()
        {
            Scenario.Define<Context>()
                .WithEndpoint<EndpointThatHostsASaga>
                (b => b.When(c => c.Subscribed, bus => bus.SendLocal(new StartSaga
                {
                    DataId = Guid.NewGuid()
                }))
                )
                .WithEndpoint<EndpointThatHandlesAMessageFromSagaAndReplies>(b => b.Given((bus, context) =>
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

        public class EndpointThatHandlesAMessageFromSagaAndReplies : EndpointConfigurationBuilder
        {
            public EndpointThatHandlesAMessageFromSagaAndReplies()
            {
                EndpointSetup<DefaultServer>(b => b.DisableFeature<AutoSubscribe>())
                    .AddMapping<DidSomething>(typeof(EndpointThatHostsASaga))
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
                    Bus.Reply(new DidSomethingResponse { DataId = message.DataId });
                }
            }
        }

        public class EndpointThatHostsASaga : EndpointConfigurationBuilder
        {
            public EndpointThatHostsASaga()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) =>
                {
                    context.Subscribed = true;
                }));
            }

            public class Saga2 : Saga<Saga2.MySaga2Data>, IAmStartedByMessages<StartSaga>, IHandleMessages<DidSomethingResponse>
            {
                public Context Context { get; set; }

                public void Handle(StartSaga message)
                {
                    Data.DataId = message.DataId;
                    Bus.Publish(new DidSomething { DataId = message.DataId });
                }

                public void Handle(DidSomethingResponse message)
                {
                    Context.DidSagaReplyMessageGetCorrelated = message.DataId == Data.DataId;
                    MarkAsComplete();
                }
                
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySaga2Data> mapper)
                {
                }

                public class MySaga2Data : ContainSagaData
                {
                    [Unique]
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
            public Guid DataId { get; set; }
        }
    }
}
