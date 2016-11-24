namespace NServiceBus.AcceptanceTests.ScaleOut
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Routing;
    using NUnit.Framework;

    public class When_replying_to_a_message_sent_to_specific_instance : NServiceBusAcceptanceTest
    {
        static string ReceiverEndpoint => Conventions.EndpointNamingConvention(typeof(Receiver));

        [Test]
        public async Task Reply_address_should_be_set_to_shared_endpoint_queue()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Receiver>()
                .WithEndpoint<Sender>(b => b.When(s => s.Send(new MyRequest())))
                .Done(c => c.ReplyToAddress != null)
                .Run();

            StringAssert.DoesNotContain("XZY", context.ReplyToAddress);
        }

        public class Context : ScenarioContext
        {
            public string ReplyToAddress { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    var routing = c.UseTransport(r.GetTransportType()).Routing();
                    routing.RouteToEndpoint(typeof(MyRequest), ReceiverEndpoint);
                    c.RegisterEndpointInstances(new EndpointInstance(ReceiverEndpoint, "XYZ"));
                });
            }

            public class MyResponseHandler : IHandleMessages<MyResponse>
            {
                public Context Context { get; set; }

                public Task Handle(MyResponse message, IMessageHandlerContext context)
                {
                    Context.ReplyToAddress = context.MessageHeaders[Headers.ReplyToAddress];
                    return Task.FromResult(0);
                }
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c => { c.MakeInstanceUniquelyAddressable("XYZ"); });
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Task Handle(MyRequest message, IMessageHandlerContext context)
                {
                    return context.Reply(new MyResponse());
                }
            }
        }

        public class MyRequest : IMessage
        {
        }

        public class MyResponse : IMessage
        {
        }
    }
}