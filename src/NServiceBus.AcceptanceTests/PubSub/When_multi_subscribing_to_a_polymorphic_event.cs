﻿namespace NServiceBus.AcceptanceTests.PubSub
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
                .WithEndpoint<Publisher1>(b => b.Given((bus, context) => SubscriptionBehavior.OnEndpointSubscribed(args =>
                {
                    context.AddTrace("Publisher1 OnEndpointSubscribed " + args.MessageType);
                    if (args.MessageType.Contains(typeof(IMyEvent).Name))
                    {
                        context.SubscribedToIMyEvent = true;
                    }

                    if (args.MessageType.Contains(typeof(MyEvent2).Name))
                    {
                        context.SubscribedToMyEvent2 = true;
                    }
                })).When(c => c.SubscribedToIMyEvent && c.SubscribedToMyEvent2, (bus, c) =>
                {
                    c.AddTrace("Publishing MyEvent1");
                    bus.Publish(new MyEvent1());
                }))
                .WithEndpoint<Publisher2>(b => b.Given((bus, context) => SubscriptionBehavior.OnEndpointSubscribed(args =>
                {
                    context.AddTrace("Publisher2 OnEndpointSubscribed " + args.MessageType);
                    if (args.MessageType.Contains(typeof(IMyEvent).Name))
                    {
                        context.SubscribedToIMyEvent = true;
                    }

                    if (args.MessageType.Contains(typeof(MyEvent2).Name))
                    {
                        context.SubscribedToMyEvent2 = true;
                    }
                })).When(c => c.SubscribedToIMyEvent && c.SubscribedToMyEvent2, (bus, c) =>
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
                        context.SubscribedToIMyEvent = true;
                        context.SubscribedToMyEvent2 = true;
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
            public bool SubscribedToIMyEvent { get; set; }
            public bool SubscribedToMyEvent2 { get; set; }
        }

        public class Publisher1 : EndpointConfigurationBuilder
        {
            public Publisher1()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class Publisher2 : EndpointConfigurationBuilder
        {
            public Publisher2()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class Subscriber1 : EndpointConfigurationBuilder
        {
            public Subscriber1()
            {
                EndpointSetup<DefaultServer>(_ => { },c => c.DisableFeature<AutoSubscribe>())
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
