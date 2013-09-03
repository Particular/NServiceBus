namespace NServiceBus.AcceptanceTests.PubSub
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_publishing_an_event_with_only_local_messagehandlers : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_trigger_the_catch_all_handler()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Publisher>(b =>
                                             b.Given((bus, context) => Subscriptions.OnEndpointSubscribed(s =>
                                                 {
                                                     context.LocalEndpointSubscribed = true;
                                                 }))
                                              .When(c => c.LocalEndpointSubscribed, bus => bus.Publish(new EventHandledByLocalEndpoint()))
                )
                    .Done(c => c.CatchAllHandlerGotTheMessage)
                    .Repeat(r => r.For(Transports.Msmq))
                    .Should(c =>
                        {
                            Assert.True(c.CatchAllHandlerGotTheMessage);
                        })

                    .Run();
        }

        public class Context : ScenarioContext
        {
            
            public bool CatchAllHandlerGotTheMessage { get; set; }

            public bool LocalEndpointSubscribed { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>(c=>Configure.Features.AutoSubscribe(s => s.DoNotRequireExplicitRouting()))
                    .AddMapping<EventHandledByLocalEndpoint>(typeof(Publisher)); //a explicit mapping is needed
            }

            class CatchAllHandler:IHandleMessages<IEvent> //not enough for auto subscribe to work
            {
                public Context Context { get; set; }
                public void Handle(IEvent message)
                {
                    Context.CatchAllHandlerGotTheMessage = true;
                }
            }

            class DummyHandler : IHandleMessages<EventHandledByLocalEndpoint> //and a explicit handler
            {
                public Context Context { get; set; }
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