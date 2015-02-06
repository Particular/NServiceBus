namespace NServiceBus.AcceptanceTests.PubSub
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_publishing_with_only_local_messagehandlers : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_trigger_the_catch_all_handler_for_message_driven_subscriptions()
        {
            Scenario.Define<Context>()
                .WithEndpoint<MessageDrivenPublisher>(b =>
                    b.When(c => c.LocalEndpointSubscribed, bus => bus.Publish(new EventHandledByLocalEndpoint())))
                .Done(c => c.CatchAllHandlerGotTheMessage)
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(c => Assert.True(c.CatchAllHandlerGotTheMessage))
                .Run();
        }

        [Test]
        public void Should_trigger_the_catch_all_handler_for_publishers_with_centralized_pubsub()
        {
            Scenario.Define<Context>()
                       .WithEndpoint<CentralizedStoragePublisher>(b =>
                       {
                           b.Given(bus => bus.Subscribe<EventHandledByLocalEndpoint>());
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
                public void Handle(IEvent message)
                {
                    Context.CatchAllHandlerGotTheMessage = true;
                }
            }

            class DummyHandler : IHandleMessages<EventHandledByLocalEndpoint> //explicit handler for the event is needed
            {
                public void Handle(EventHandledByLocalEndpoint message)
                {
                }
            }
        }

        public class CentralizedStoragePublisher : EndpointConfigurationBuilder
        {
            public CentralizedStoragePublisher()
            {
                EndpointSetup<DefaultServer>();
            }

            class CatchAllHandler : IHandleMessages<IEvent> 
            {
                public Context Context { get; set; }
                public void Handle(IEvent message)
                {
                    Context.CatchAllHandlerGotTheMessage = true;
                }
            }

            class DummyHandler : IHandleMessages<EventHandledByLocalEndpoint> //explicit handler for the event is needed
            {
                public void Handle(EventHandledByLocalEndpoint message)
                {
                }
            }
        }
        [Serializable]
        public class EventHandledByLocalEndpoint : IEvent
        {
        }
    }
}