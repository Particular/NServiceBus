namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_publishing_with_only_local_messagehandlers : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_trigger_the_catch_all_handler_for_message_driven_subscriptions()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<MessageDrivenPublisher>(b =>
                    b.When(c => c.LocalEndpointSubscribed, bus => bus.Publish(new EventHandledByLocalEndpoint())))
                .Done(c => c.CatchAllHandlerGotTheMessage)
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(c => Assert.True(c.CatchAllHandlerGotTheMessage))
                .Run();
        }

        [Test]
        public async Task Should_trigger_the_catch_all_handler_for_publishers_with_centralized_pubsub()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<CentralizedStoragePublisher>(b =>
                {
                    b.When(bus => bus.Subscribe<EventHandledByLocalEndpoint>());
                    b.When(c => c.EndpointsStarted, (bus, context) => bus.Publish(new EventHandledByLocalEndpoint()));
                })
                .Done(c => c.CatchAllHandlerGotTheMessage)
                .Repeat(r => r.For<AllTransportsWithCentralizedPubSubSupport>())
                .Should(c => Assert.True(c.CatchAllHandlerGotTheMessage))
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool CatchAllHandlerGotTheMessage { get; set; }

            public bool LocalEndpointSubscribed { get; set; }
        }

        public class MessageDrivenPublisher : EndpointConfigurationBuilder
        {
            public MessageDrivenPublisher()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) =>
                {
                    context.LocalEndpointSubscribed = true;
                }))
                .AddMapping<EventHandledByLocalEndpoint>(typeof(MessageDrivenPublisher)); //an explicit mapping is needed
            }

            class CatchAllHandler:IHandleMessages<IEvent> //not enough for auto subscribe to work
            {
                public Context Context { get; set; }
                public Task Handle(IEvent message, IMessageHandlerContext context)
                {
                    Context.CatchAllHandlerGotTheMessage = true;

                    return Task.FromResult(0);
                }
            }

            class DummyHandler : IHandleMessages<EventHandledByLocalEndpoint> //explicit handler for the event is needed
            {
                public Task Handle(EventHandledByLocalEndpoint message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class CentralizedStoragePublisher : EndpointConfigurationBuilder
        {
            public CentralizedStoragePublisher()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<EventHandledByLocalEndpoint>(typeof(CentralizedStoragePublisher)); //an explicit mapping may be needed, depends on the technology underneath;
            }

            class CatchAllHandler : IHandleMessages<IEvent> 
            {
                public Context Context { get; set; }
                public Task Handle(IEvent message, IMessageHandlerContext context)
                {
                    Context.CatchAllHandlerGotTheMessage = true;
                    return Task.FromResult(0);
                }
            }

            class DummyHandler : IHandleMessages<EventHandledByLocalEndpoint> //explicit handler for the event is needed
            {
                public Task Handle(EventHandledByLocalEndpoint message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }
        [Serializable]
        public class EventHandledByLocalEndpoint : IEvent
        {
        }
    }
}