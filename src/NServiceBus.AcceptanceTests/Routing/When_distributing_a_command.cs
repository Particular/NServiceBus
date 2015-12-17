namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Routing;
    using NUnit.Framework;

    public class When_distributing_a_command : NServiceBusAcceptanceTest
    {
        [Test, Explicit("Flaky on the buildserver - https://github.com/Particular/NServiceBus/issues/3003")]
        public async Task Should_round_robin()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When((bus, c) => bus.Send(new Request())))
                .WithEndpoint<Receiver1>()
                .WithEndpoint<Receiver2>()
                .Done(c => c.Receiver1TimesCalled > 4 && c.Receiver2TimesCalled > 4)
                .Run();

            Assert.IsTrue(context.Receiver1TimesCalled > 4);
            Assert.IsTrue(context.Receiver2TimesCalled > 4);
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
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "routes.xml");
                File.WriteAllText(filePath, @"<endpoints>
    <endpoint name=""DistributingACommand.Receiver"">
        <instance discriminator=""1""/>
        <instance discriminator=""2""/>
    </endpoint>
</endpoints>
");
                
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Routing().UseFileBasedEndpointInstanceMapping(filePath);
                    c.Routing().UnicastRoutingTable.RouteToEndpoint(typeof(Request), new EndpointName("DistributingACommand.Receiver"));
                });
            }

            public class ResponseHandler : IHandleMessages<Response>
            {
                public Context Context { get; set; }

                public Task Handle(Response message, IMessageHandlerContext context)
                {
                    switch (message.EndpointName)
                    {
                        case "Receiver1":
                            Context.Receiver1TimesCalled++;
                            break;
                        case "Receiver2":
                            Context.Receiver2TimesCalled++;
                            break;
                    }

                    return context.Send(new Request());
                }
            }
        }

        public class Receiver1 : EndpointConfigurationBuilder
        {
            public Receiver1()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EndpointName("DistributingACommand.Receiver");
                    c.ScaleOut().InstanceDiscriminator("1");
                });
            }

            public class MyMessageHandler : IHandleMessages<Request>
            {
                public Task Handle(Request message, IMessageHandlerContext context)
                {
                    return context.Reply(new Response
                    {
                        EndpointName = "Receiver1"
                    });
                }
            }
        }

        public class Receiver2 : EndpointConfigurationBuilder
        {
            public Receiver2()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EndpointName("DistributingACommand.Receiver");
                    c.ScaleOut().InstanceDiscriminator("2");
                });
            }

            public class MyMessageHandler : IHandleMessages<Request>
            {
                public Task Handle(Request message, IMessageHandlerContext context)
                {
                    return context.Reply(new Response
                    {
                        EndpointName = "Receiver2"
                    });
                }
            }
        }

        [Serializable]
        public class Request : ICommand
        {
        }

        [Serializable]
        public class Response : IMessage
        {
            public string EndpointName { get; set; }
        }
    }
}