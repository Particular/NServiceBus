namespace NServiceBus.AcceptanceTests.ScaleOut
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Routing;
    using NServiceBus.Routing.Legacy;
    using NUnit.Framework;

    public class When_replying_to_a_message_sent_via_a_distributor : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Reply_address_should_be_set_to_that_specific_instance()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Receiver>()
                .WithEndpoint<Sender>(b => b.When(s => s.Send(new MyRequest())))
                .Done(c => c.ReplyToAddress != null)
                .Run();

            StringAssert.Contains("Distributor", context.ReplyToAddress);
        }

        static string ReceiverEndpoint => Conventions.EndpointNamingConvention(typeof(Receiver));

        public class Context : ScenarioContext
        {
            public string ReplyToAddress { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Routing().UnicastRoutingTable.RouteToEndpoint(typeof(MyRequest), ReceiverEndpoint);
                    c.Routing().EndpointInstances.AddStatic(new EndpointName(ReceiverEndpoint), new EndpointInstance(ReceiverEndpoint, "XYZ"));
                    c.AddHeaderToAllOutgoingMessages("NServiceBus.Distributor.WorkerSessionId", "SomeID");
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
                EndpointSetup<DefaultServer>(c =>
                {
                    c.ScaleOut().InstanceDiscriminator("XYZ");
                    c.EnlistWithLegacyMSMQDistributor("Distributor", ReceiverEndpoint, 1);
                });
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