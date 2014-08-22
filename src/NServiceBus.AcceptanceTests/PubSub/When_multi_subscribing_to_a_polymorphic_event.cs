namespace NServiceBus.AcceptanceTests.PubSub
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_multi_subscribing_to_a_polymorphic_event : NServiceBusAcceptanceTest
    {
        [Test]
        public void Both_events_should_be_delivered()
        {
            var rootContext = new Context();

            Scenario.Define(rootContext)
                .WithEndpoint<Publisher1>(b => b.When(c => c.Publisher1HasASubscriberForIMyEvent, (bus, c) =>
                {
                    c.AddTrace("Publishing MyEvent1");
                    bus.Publish(new MyEvent1());
                }))
                .WithEndpoint<Publisher2>(b => b.When(c => c.Publisher2HasDetectedASubscriberForEvent2, (bus, c) =>
                {
                    c.AddTrace("Publishing MyEvent2");
                    bus.Publish(new MyEvent2());
                }))
                .WithEndpoint<Subscriber1>(b => b.Given((bus, context) =>
                {
                    context.AddTrace("Subscriber1 subscribing to both events");
                    bus.Subscribe<IMyEvent>();
                    bus.Subscribe<MyEvent2>();

                    if (context.HasNativePubSubSupport)
                    {
                        context.Publisher1HasASubscriberForIMyEvent = true;
                        context.Publisher2HasDetectedASubscriberForEvent2 = true;
                    }
                }))
                .AllowExceptions(e => e.Message.Contains("Oracle.DataAccess.Client.OracleException: ORA-00001") || e.Message.Contains("System.Data.SqlClient.SqlException: Violation of PRIMARY KEY constraint"))
                .Done(c => c.SubscriberGotIMyEvent && c.SubscriberGotMyEvent2)
                .Run();

            Assert.True(rootContext.SubscriberGotIMyEvent);
            Assert.True(rootContext.SubscriberGotMyEvent2);
        }

        public class Context : ScenarioContext
        {
            public bool SubscriberGotIMyEvent { get; set; }
            public bool SubscriberGotMyEvent2 { get; set; }
            public bool Publisher1HasASubscriberForIMyEvent { get; set; }
            public bool Publisher2HasDetectedASubscriberForEvent2 { get; set; }
        }

        public class Publisher1 : EndpointConfigurationBuilder
        {
            public Publisher1()
            {
                EndpointSetup<DefaultPublisher>( b => b.OnEndpointSubscribed<Context>((args, context) =>
                {
                    context.AddTrace("Publisher1 OnEndpointSubscribed " + args.MessageType);
                    if (args.MessageType.Contains(typeof(IMyEvent).Name))
                    {
                        context.Publisher1HasASubscriberForIMyEvent = true;
                    }
                }));
            }
        }

        public class Publisher2 : EndpointConfigurationBuilder
        {
            public Publisher2()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((args, context) =>
                {
                    context.AddTrace("Publisher2 OnEndpointSubscribed " + args.MessageType);

                    if (args.MessageType.Contains(typeof(MyEvent2).Name))
                    {
                        context.Publisher2HasDetectedASubscriberForEvent2 = true;
                    }
                }));
            }
        }

        public class Subscriber1 : EndpointConfigurationBuilder
        {
            public Subscriber1()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>())
                    .AddMapping<IMyEvent>(typeof(Publisher1))
                    .AddMapping<MyEvent2>(typeof(Publisher2));
            }

            public class MyEventHandler : IHandleMessages<IMyEvent>
            {
                public Context Context { get; set; }

                public void Handle(IMyEvent messageThatIsEnlisted)
                {
                    Context.AddTrace(String.Format("Got event '{0}'", messageThatIsEnlisted));
                    if (messageThatIsEnlisted is MyEvent2)
                    {
                        Context.SubscriberGotMyEvent2 = true;
                    }
                    else
                    {
                        Context.SubscriberGotIMyEvent = true;
                    }
                }
            }
        }
        
        [Serializable]
        public class MyEvent1 : IMyEvent
        {
        }

        [Serializable]
        public class MyEvent2 : IMyEvent
        {
        }

        public interface IMyEvent : IEvent
        {
        }
    }
}
