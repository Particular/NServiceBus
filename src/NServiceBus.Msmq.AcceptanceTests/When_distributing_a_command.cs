﻿namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using Configuration.AdvanceExtensibility;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Settings;

    public class When_distributing_a_command : NServiceBusAcceptanceTest
    {
        static string ReceiverAEndpoint => Conventions.EndpointNamingConvention(typeof(ReceiverA));
        static string ReceiverBEndpoint => Conventions.EndpointNamingConvention(typeof(ReceiverB));

        [Test]
        public async Task Should_round_robin()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When((session, c) => session.Send(new RequestA())))
                .WithEndpoint<ReceiverA>(b => b.CustomConfig(c =>
                {
                    c.ScaleOut().InstanceDiscriminator("1");
                    c.GetSettings().Set("Name", "ReceiverA1");
                }))
                .WithEndpoint<ReceiverA>(b => b.CustomConfig(c =>
                {
                    c.ScaleOut().InstanceDiscriminator("2");
                    c.GetSettings().Set("Name", "ReceiverA2");
                }))
                .WithEndpoint<ReceiverB>(b => b.CustomConfig(c =>
                {
                    c.ScaleOut().InstanceDiscriminator("1");
                    c.GetSettings().Set("Name", "ReceiverB1");
                }))
                .WithEndpoint<ReceiverB>(b => b.CustomConfig(c =>
                {
                    c.ScaleOut().InstanceDiscriminator("2");
                    c.GetSettings().Set("Name", "ReceiverB2");
                }))
                .Done(c => c.ReceiverA1TimesCalled > 4 || c.ReceiverA2TimesCalled > 4 || c.ReceiverB1TimesCalled > 4 || c.ReceiverB2TimesCalled > 4)
                .Run();

            Assert.IsTrue(context.ReceiverA1TimesCalled > 3);
            Assert.IsTrue(context.ReceiverA2TimesCalled > 3);
            Assert.IsTrue(context.ReceiverB1TimesCalled > 3);
            Assert.IsTrue(context.ReceiverB2TimesCalled > 3);
        }

        public class Context : ScenarioContext
        {
            public int ReceiverA1TimesCalled { get; set; }
            public int ReceiverA2TimesCalled { get; set; }
            public int ReceiverB1TimesCalled { get; set; }
            public int ReceiverB2TimesCalled { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "routes.xml");
                File.WriteAllText(filePath, string.Format(@"<endpoints>
    <endpoint name=""{0}"">
        <instance discriminator=""1""/>
        <instance discriminator=""2""/>
    </endpoint>
    <endpoint name=""{1}"">
        <instance discriminator=""1""/>
        <instance discriminator=""2""/>
    </endpoint>
</endpoints>
", ReceiverAEndpoint, ReceiverBEndpoint));

                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<MsmqTransport>().DistributeMessagesUsingFileBasedEndpointInstanceMapping(filePath);
                    c.UnicastRouting().RouteToEndpoint(typeof(RequestA), ReceiverAEndpoint);
                    c.UnicastRouting().RouteToEndpoint(typeof(RequestB), ReceiverBEndpoint);
                });
            }

            public class ResponseHandler : 
                IHandleMessages<ResponseA>,
                IHandleMessages<ResponseB>
            {
                public Context Context { get; set; }

                public Task Handle(ResponseA message, IMessageHandlerContext context)
                {
                    switch (message.EndpointName)
                    {
                        case "ReceiverA1":
                            Context.ReceiverA1TimesCalled++;
                            break;
                        case "ReceiverA2":
                            Context.ReceiverA2TimesCalled++;
                            break;
                    }

                    return context.Send(new RequestB());
                }

                public Task Handle(ResponseB message, IMessageHandlerContext context)
                {
                    switch (message.EndpointName)
                    {
                        case "ReceiverB1":
                            Context.ReceiverB1TimesCalled++;
                            break;
                        case "ReceiverB2":
                            Context.ReceiverB2TimesCalled++;
                            break;
                    }

                    return context.Send(new RequestA());
                }
            }
        }

        public class ReceiverA : EndpointConfigurationBuilder
        {
            public ReceiverA()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IHandleMessages<RequestA>
            {
                public ReadOnlySettings Settings { get; set; }

                public Task Handle(RequestA message, IMessageHandlerContext context)
                {
                    return context.Reply(new ResponseA
                    {
                        EndpointName = Settings.Get<string>("Name")
                    });
                }
            }
        }

        public class ReceiverB : EndpointConfigurationBuilder
        {
            public ReceiverB()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IHandleMessages<RequestB>
            {
                public ReadOnlySettings Settings { get; set; }

                public Task Handle(RequestB message, IMessageHandlerContext context)
                {
                    return context.Reply(new ResponseB
                    {
                        EndpointName = Settings.Get<string>("Name")
                    });
                }
            }
        }

        public class RequestA : ICommand
        {
        }

        public class RequestB : ICommand
        {
        }

        public class ResponseA : IMessage
        {
            public string EndpointName { get; set; }
        }

        public class ResponseB : IMessage
        {
            public string EndpointName { get; set; }
        }
    }
}