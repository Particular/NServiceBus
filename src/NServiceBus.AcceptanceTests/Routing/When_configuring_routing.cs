namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;


    public class When_configuring_routing : NServiceBusAcceptanceTest
    {
        static string ReceiverEndpoint => Conventions.EndpointNamingConvention(typeof(Receiver));

        [Test]
        public async Task Sends_a_message_if_logical_address_provided()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Receiver>()
                .WithEndpoint<SenderUsingLogicalAddress>(b => b.When(s => s.Send(new MyMessage())))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.IsTrue(context.MessageReceived);
        }

        [Test]
        public void Throws_exception_if_endpoint_name_provided()
        {
            var ae = Assert.ThrowsAsync<AggregateException>(async () =>
                            await Scenario.Define<Context>()
                                .WithEndpoint<Receiver>()
                                .WithEndpoint<SenderUsingPhysicalAddress>(b => b.When(s => s.Send(new MyMessage())))
                                .Done(c => c.MessageReceived)
                                .Run());

            var expected = $"Expected an endpoint name but received '{ReceiverEndpoint}@localhost'. Use routing file to specify physical address of the endpoint.";
            var outerExc = ae.InnerExceptions.Single(ex => ex.Message == "Endpoint ConfiguringRouting.SenderUsingPhysicalAddress failed to initialize");

            Assert.AreEqual(typeof(ArgumentException), outerExc.InnerException.GetType());
            Assert.AreEqual(expected, outerExc.InnerException.Message);
        }


        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }

        class SenderUsingLogicalAddress : EndpointConfigurationBuilder
        {
            public SenderUsingLogicalAddress()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.UseTransport(r.GetTransportType()).Routing().RouteToEndpoint(typeof(MyMessage), ReceiverEndpoint);
                });
            }
        }

        class SenderUsingPhysicalAddress : EndpointConfigurationBuilder
        {
            public SenderUsingPhysicalAddress()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.UseTransport(r.GetTransportType()).Routing().RouteToEndpoint(typeof(MyMessage), $"{ReceiverEndpoint}@localhost");
                });
            }
        }

        class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    Context.MessageReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}
