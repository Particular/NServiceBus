﻿namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Settings;

    public class When_distributing_a_command : NServiceBusAcceptanceTest
    {
        const int numberOfMessagesToSendPerEndpoint = 20;
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
                }))
                .WithEndpoint<ReceiverA>(b => b.CustomConfig(c =>
                {
                    c.ScaleOut().InstanceDiscriminator("2");
                }))
                .WithEndpoint<ReceiverB>(b => b.CustomConfig(c =>
                {
                    c.ScaleOut().InstanceDiscriminator("1");
                }))
                .WithEndpoint<ReceiverB>(b => b.CustomConfig(c =>
                {
                    c.ScaleOut().InstanceDiscriminator("2");
                }))
                .Done(c => c.MessagesReceivedPerEndpoint == numberOfMessagesToSendPerEndpoint)
                .Run();

            Assert.That(context.ReceiverA1TimesCalled, Is.EqualTo(10));
            Assert.That(context.ReceiverA2TimesCalled, Is.EqualTo(10));
            Assert.That(context.ReceiverB1TimesCalled, Is.EqualTo(10));
            Assert.That(context.ReceiverB2TimesCalled, Is.EqualTo(10));
        }

        public class Context : ScenarioContext
        {
            public int MessagesReceivedPerEndpoint { get; set; }
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
                Context testContext;

                public ResponseHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(ResponseA message, IMessageHandlerContext context)
                {
                    switch (message.EndpointInstance)
                    {
                        case "1":
                            testContext.ReceiverA1TimesCalled++;
                            break;
                        case "2":
                            testContext.ReceiverA2TimesCalled++;
                            break;
                    }

                    return context.Send(new RequestB());
                }

                public Task Handle(ResponseB message, IMessageHandlerContext context)
                {
                    switch (message.EndpointInstance)
                    {
                        case "1":
                            testContext.ReceiverB1TimesCalled++;
                            break;
                        case "2":
                            testContext.ReceiverB2TimesCalled++;
                            break;
                    }

                    testContext.MessagesReceivedPerEndpoint++;
                    if (testContext.MessagesReceivedPerEndpoint < numberOfMessagesToSendPerEndpoint)
                    {
                        return context.Send(new RequestA());
                    }

                    return Task.CompletedTask;
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
                        EndpointInstance = Settings.Get<string>("EndpointInstanceDiscriminator")
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
                        EndpointInstance = Settings.Get<string>("EndpointInstanceDiscriminator")
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
            public string EndpointInstance { get; set; }
        }

        public class ResponseB : IMessage
        {
            public string EndpointInstance { get; set; }
        }
    }
}