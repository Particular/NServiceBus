namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using NServiceBus.Routing;
    using NUnit.Framework;

    public class When_making_endpoint_uniquely_addressable : NServiceBusAcceptanceTest
    {
        static string ReceiverEndpoint => Conventions.EndpointNamingConvention(typeof(Receiver));
        const string InstanceDiscriminator = "XYZ";

        [Test]
        public async Task Should_be_addressable_both_by_shared_queue_and_unique_queue()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Receiver>()
                .WithEndpoint<UnawareOfInstanceSender>(b => b.When(s => s.Send(new MyMessage())))
                .WithEndpoint<InstanceAwareSender>(b => b.When(s => s.Send(new MyMessage())))
                .Done(c => c.MessagesReceived > 1)
                .Run();

            Assert.AreEqual(2, context.MessagesReceived);
        }

        public class Context : ScenarioContext
        {
            public int MessagesReceived;
        }

        public class UnawareOfInstanceSender : EndpointConfigurationBuilder
        {
            public UnawareOfInstanceSender()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.ConfigureRouting().RouteToEndpoint(typeof(MyMessage), ReceiverEndpoint);
                });
            }
        }

        public class InstanceAwareSender : EndpointConfigurationBuilder
        {
            public InstanceAwareSender()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.ConfigureRouting().RouteToEndpoint(typeof(MyMessage), ReceiverEndpoint);
                    c.GetSettings().GetOrCreate<EndpointInstances>()
                        .AddOrReplaceInstances("testing", new List<EndpointInstance>
                        {
                            new EndpointInstance(ReceiverEndpoint, InstanceDiscriminator)
                        });
                });
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c => { c.MakeInstanceUniquelyAddressable(InstanceDiscriminator); });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public MyMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    Interlocked.Increment(ref testContext.MessagesReceived);
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}