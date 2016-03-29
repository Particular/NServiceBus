namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Routing;
    using NUnit.Framework;

    public class When_broadcasting_a_command : NServiceBusAcceptanceTest
    {
        static string ReceiverEdndpoint => Conventions.EndpointNamingConvention(typeof(Receiver));

        [Test]
        public async Task Should_send_it_to_all_instances()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When(c => c.EndpointsStarted, (session, c) => session.Send(new Request())))
                .WithEndpoint<Receiver>(b => { b.CustomConfig(c => c.ScaleOut().InstanceDiscriminator("1")); })
                .WithEndpoint<Receiver>(b => { b.CustomConfig(c => c.ScaleOut().InstanceDiscriminator("2")); })
                .Done(c => c.Receiver1TimesCalled > 0 && c.Receiver2TimesCalled > 0)
                .Run();

            Assert.AreEqual(1, context.Receiver1TimesCalled);
            Assert.AreEqual(1, context.Receiver2TimesCalled);
        }

        public class Context : ScenarioContext
        {
            public int Receiver1TimesCalled { get; set; }
            public int Receiver2TimesCalled { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UnicastRouting().RouteToEndpoint(typeof(Request), ReceiverEdndpoint);

                    c.UnicastRouting().Mapping.SetMessageDistributionStrategy(new AllInstancesDistributionStrategy(), t => t == typeof(Request));
                    c.UnicastRouting().Mapping.Physical.Add(new EndpointName(ReceiverEdndpoint),
                        new EndpointInstance(ReceiverEdndpoint, "1"),
                        new EndpointInstance(ReceiverEdndpoint, "2"));
                });
            }

            public class ResponseHandler : IHandleMessages<Response>
            {
                public Context Context { get; set; }

                public Task Handle(Response message, IMessageHandlerContext context)
                {
                    var messageHeader = context.MessageHeaders[Headers.ReplyToAddress];
                    if (messageHeader.Contains("Receiver") && messageHeader.Contains("1"))
                    {
                        Context.Receiver1TimesCalled++;
                    }
                    else if (messageHeader.Contains("Receiver") && messageHeader.Contains("2"))
                    {
                        Context.Receiver2TimesCalled++;
                    }
                    return Task.FromResult(0);
                }
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IHandleMessages<Request>
            {
                public Task Handle(Request message, IMessageHandlerContext context)
                {
                    var options = new ReplyOptions();
                    options.RouteReplyToThisInstance();
                    return context.Reply(new Response(), options);
                }
            }
        }

        public class Request : ICommand
        {
        }

        public class Response : IMessage
        {
        }
    }
}