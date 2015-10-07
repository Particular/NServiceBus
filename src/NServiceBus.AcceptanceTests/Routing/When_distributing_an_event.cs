namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_distributing_an_event : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_round_robin()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Distributor>(b => b.When(async (bus, c) =>
                {
                    await bus.SendLocalAsync(new MyRequest());
                    await bus.SendLocalAsync(new MyRequest());
                }))
                .WithEndpoint<SubscriberA_1>()
                .WithEndpoint<SubscriberB_1>()
                .Done(c => c.SubscriberACounter >= 2 && c.SubscriberBCounter >= 2)
                .Run();

            Assert.IsTrue(context.SubscriberACounter >= 2);
            Assert.IsTrue(context.SubscriberBCounter >= 2);
        }

        public class Context : ScenarioContext
        {
            int subscriberACounter;
            int subscriberBCounter;

            public int SubscriberACounter => subscriberACounter;

            public int SubscriberBCounter => subscriberBCounter;

            public void IncrementSubscriberACounter()
            {
                Interlocked.Increment(ref subscriberACounter);
            }
            
            public void IncrementSubscriberBCounter()
            {
                Interlocked.Increment(ref subscriberBCounter);
            }
        }

        public class Distributor : EndpointConfigurationBuilder
        {
            public Distributor()
            {
                var basePath = AppDomain.CurrentDomain.BaseDirectory;

                File.WriteAllLines(Path.Combine(basePath, "DynamicRouting.SubscriberA.1.txt"), new[]
                {
                    "DynamicRouting.SubscriberA.1",
                    "DynamicRouting.SubscriberA.2"
                });
                
                File.WriteAllLines(Path.Combine(basePath, "DynamicRouting.SubscriberB.1.txt"), new[]
                {
                    "DynamicRouting.SubscriberB.1",
                    "DynamicRouting.SubscriberB.2"
                });

                EndpointSetup<DefaultServer>(c => c.Routing().UseFileBasedEndpointInstanceLists()
                    .LookForFilesIn(basePath));
            }

            public class ResponseHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public Task Handle(MyRequest message)
                {
                    return Bus.PublishAsync(new MyEvent());
                }
            }
        }

        public class SubscriberA_1 : EndpointConfigurationBuilder
        {
            public SubscriberA_1()
            {
                EndpointSetup<DefaultServer>(c => c.EndpointName("DynamicRouting.SubscriberA.1"))
                    .AddMapping<MyEvent>(typeof(Distributor));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public Task Handle(MyEvent message)
                {
                    Context.IncrementSubscriberACounter();
                    return Task.FromResult(0);
                }
            }
        }
        
        public class SubscriberA_2 : EndpointConfigurationBuilder
        {
            public SubscriberA_2()
            {
                EndpointSetup<DefaultServer>(c => c.EndpointName("DynamicRouting.SubscriberA.2"))
                    .AddMapping<MyEvent>(typeof(Distributor));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public Task Handle(MyEvent message)
                {
                    Context.IncrementSubscriberACounter();
                    return Task.FromResult(0);
                }
            }
        }

        public class SubscriberB_1 : EndpointConfigurationBuilder
        {
            public SubscriberB_1()
            {
                EndpointSetup<DefaultServer>(c => c.EndpointName("DynamicRouting.SubscriberB.1"))
                    .AddMapping<MyEvent>(typeof(Distributor));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public Task Handle(MyEvent message)
                {
                    Context.IncrementSubscriberBCounter();
                    return Task.FromResult(0);
                }
            }
        }
        
        public class SubscriberB_2 : EndpointConfigurationBuilder
        {
            public SubscriberB_2()
            {
                EndpointSetup<DefaultServer>(c => c.EndpointName("DynamicRouting.SubscriberB.2"))
                    .AddMapping<MyEvent>(typeof(Distributor));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public Task Handle(MyEvent message)
                {
                    Context.IncrementSubscriberBCounter();
                    return Task.FromResult(0);
                }
            }
        }

        public class MyRequest : ICommand
        {
        }
        
        public class MyEvent : IEvent
        {
        }
    }
}
