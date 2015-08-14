namespace NServiceBus.AcceptanceTests.ApiExtension
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.Routing;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Extensibility;
    using NServiceBus.Features;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;

    public class When_extending_the_publish_api : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_make_the_context_available_to_behaviors()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Publisher>(b =>
                        b.When(c => c.Subscriber1Subscribed, bus =>
                        {
                            var options = new PublishOptions();

                            options.GetExtensions().Set(new Publisher.PublishExtensionBehavior.Context { SomeProperty = "ItWorks" });

                            return bus.Publish(new MyEvent(), options);
                        })
                     )
                    .WithEndpoint<Subscriber1>(b => b.Given((bus, context) =>
                        {
                            bus.Subscribe<MyEvent>();

                            if (context.HasNativePubSubSupport)
                                context.Subscriber1Subscribed = true;

                            return Task.FromResult(true);
                        }))
                    .Done(c => c.Subscriber1GotTheEvent)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.True(c.Subscriber1GotTheEvent))

                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool Subscriber1GotTheEvent { get; set; }
            public bool Subscriber1Subscribed { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.OnEndpointSubscribed<Context>((s, context) =>
                    {
                        context.Subscriber1Subscribed = true;
                    });

                    b.Pipeline.Register("PublishExtensionBehavior", typeof(PublishExtensionBehavior), "Testing publish extensions");
                });
            }

            public class PublishExtensionBehavior : Behavior<OutgoingContext>
            {
                public override Task Invoke(OutgoingContext context, Func<Task> next)
                {
                    Context data;

                    if (context.TryGet(out data))
                    {
                        Assert.AreEqual("ItWorks", data.SomeProperty);
                    }
                    else
                    {
                        Assert.Fail("Expected to find the data!");
                    }

                    return next();
                }

               public  class Context
                {
                    public string SomeProperty { get; set; }
                }
            }

         
        }

     

        public class Subscriber1 : EndpointConfigurationBuilder
        {
            public Subscriber1()
            {
                EndpointSetup<DefaultServer>(builder =>
                {
                    builder.DisableFeature<AutoSubscribe>();
                })
                    .AddMapping<MyEvent>(typeof(Publisher));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public void Handle(MyEvent messageThatIsEnlisted)
                {
                    Context.Subscriber1GotTheEvent = true;
                }
            }
        }

        [Serializable]
        public class MyEvent : IEvent
        {
        }
    }

}
