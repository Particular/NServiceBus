﻿namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_publishing_with_only_local_messagehandlers : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_trigger_the_catch_all_handler_for_message_driven_subscriptions()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<MessageDrivenPublisher>(b =>
                    b.When(c => c.LocalEndpointSubscribed, session => session.Publish(new EventHandledByLocalEndpoint())))
                .Done(c => c.CatchAllHandlerGotTheMessage)
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(c => Assert.True(c.CatchAllHandlerGotTheMessage))
                .Run();
        }

        [Test]
        public Task Should_trigger_the_catch_all_handler_for_publishers_with_centralized_pubsub()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<CentralizedStoragePublisher>(b =>
                {
                    b.When(session => session.Subscribe<EventHandledByLocalEndpoint>());
                    b.When(c => c.EndpointsStarted, (session, context) => session.Publish(new EventHandledByLocalEndpoint()));
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
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) => { context.LocalEndpointSubscribed = true; }),
                    metadata => metadata.RegisterPublisherFor<EventHandledByLocalEndpoint>(typeof(MessageDrivenPublisher)));
            }

            class CatchAllHandler : IHandleMessages<IEvent> //not enough for auto subscribe to work
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
                EndpointSetup<DefaultServer>(publisherMetadata: metadata => metadata.RegisterPublisherFor<EventHandledByLocalEndpoint>(typeof(CentralizedStoragePublisher)));
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


        public class EventHandledByLocalEndpoint : IEvent
        {
        }
    }
}