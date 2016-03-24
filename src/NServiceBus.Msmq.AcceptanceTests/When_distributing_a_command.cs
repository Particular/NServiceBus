namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using Configuration.AdvanceExtensibility;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;
    using Settings;

    public class When_distributing_a_command : NServiceBusAcceptanceTest
    {
        static string ReceiverEdndpoint => Conventions.EndpointNamingConvention(typeof(Receiver));

        [Test]
        public async Task Should_round_robin()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When((session, c) => session.Send(new Request())))
                .WithEndpoint<Receiver>(b => b.CustomConfig(c =>
                {
                    c.ScaleOut().InstanceDiscriminator("1");
                    c.GetSettings().Set("Name", "Receiver1");
                }))
                .WithEndpoint<Receiver>(b => b.CustomConfig(c =>
                {
                    c.ScaleOut().InstanceDiscriminator("2");
                    c.GetSettings().Set("Name", "Receiver2");
                }))
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
                    c.UseTransport<MsmqTransport>().DistributeMessagesUsingFileBasedEndpointInstanceMapping(filePath);
                    c.UnicastRouting().RouteToEndpoint(typeof(Request), ReceiverEdndpoint);
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

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IHandleMessages<Request>
            {
                public ReadOnlySettings Settings { get; set; }

                public Task Handle(Request message, IMessageHandlerContext context)
                {
                    return context.Reply(new Response
                    {
                        EndpointName = Settings.Get<string>("Name")
                    });
                }
            }
        }

        public class Request : ICommand
        {
        }

        public class Response : IMessage
        {
            public string EndpointName { get; set; }
        }
    }
}