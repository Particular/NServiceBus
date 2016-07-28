﻿namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Routing;
    using NUnit.Framework;

    public class When_broadcasting_a_command : NServiceBusAcceptanceTest
    {
        static string ReceiverEndpoint => Conventions.NameOf<Receiver>();

        [Test]
        public async Task Should_send_it_to_all_instances()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When(c => c.EndpointsStarted, (session, c) => session.Send(new Request())))
                .WithEndpoint<Receiver>(b => { b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("1")); })
                .WithEndpoint<Receiver>(b => { b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("2")); })
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
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.UseTransport(r.GetTransportType()).Routing().RouteToEndpoint(typeof(Request), ReceiverEndpoint);
                    c.EnableFeature<SpecificRoutingFeature>();
                });
            }

            class SpecificRoutingFeature : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.DistributionPolicy().SetDistributionStrategy(ReceiverEndpoint, new AllInstancesDistributionStrategy());
                    context.EndpointInstances().Add(
                        new EndpointInstance(ReceiverEndpoint, "1"),
                        new EndpointInstance(ReceiverEndpoint, "2"));
                }
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