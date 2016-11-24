namespace NServiceBus.AcceptanceTests.ScaleOut
{
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Routing;
    using NUnit.Framework;

    public class When_using_instance_ids : NServiceBusAcceptanceTest
    {
        static string ReceiverEndpoint => Conventions.EndpointNamingConvention(typeof(Receiver));

        [Test]
        public async Task Should_be_addressable_both_by_shared_queue_and_specific_queue()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Receiver>()
                .WithEndpoint<UnawareSender>(b => b.When(s => s.Send(new MyMessage())))
                .WithEndpoint<AwareSender>(b => b.When(s => s.Send(new MyMessage())))
                .Done(c => c.MessagesReceived > 1)
                .Run();

            Assert.AreEqual(2, context.MessagesReceived);
        }

        public class Context : ScenarioContext
        {
            public int MessagesReceived;
        }

        public class UnawareSender : EndpointConfigurationBuilder
        {
            public UnawareSender()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.UseTransport(r.GetTransportType()).Routing().RouteToEndpoint(typeof(MyMessage), ReceiverEndpoint);
                });
            }
        }

        public class AwareSender : EndpointConfigurationBuilder
        {
            public AwareSender()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    var routing = c.UseTransport(r.GetTransportType()).Routing();
                    routing.RouteToEndpoint(typeof(MyMessage), ReceiverEndpoint);
                    c.RegisterEndpointInstances(new EndpointInstance(ReceiverEndpoint, "XYZ"));
                });
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c => { c.MakeInstanceUniquelyAddressable("XYZ"); });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    Interlocked.Increment(ref Context.MessagesReceived);
                    return Task.FromResult(0);
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}