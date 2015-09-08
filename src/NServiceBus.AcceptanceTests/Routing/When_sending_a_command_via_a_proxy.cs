namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NUnit.Framework;

    public class When_sending_a_command_via_a_proxy : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_send_via_a_proxy_but_reply_directly()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When(c => c.ReceiverReady && c.ProxyReady, (bus, c) => bus.SendAsync(new Request())))
                .WithEndpoint<Receiver>()
                .WithEndpoint<Proxy>()
                .Done(c => c.SenderGotResponse)
                .Run();

            Assert.IsTrue(context.SenderGotResponse);
            Assert.IsTrue(context.ReceiverGotRequest);
            Assert.IsTrue(context.ProxyGotRequest);
            Assert.IsFalse(context.ProxyGotResponse);
        }

        public class Context : ScenarioContext
        {
            public bool ProxyGotRequest { get; set; }
            public bool ReceiverGotRequest { get; set; }
            public bool ProxyGotResponse { get; set; }
            public bool SenderGotResponse { get; set; }

            public bool ProxyReady { get; set; }
            public bool ReceiverReady { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var receiverEndpoint = new EndpointName("SendingACommandViaAProxy.Receiver");
                    var proxyEndpoint = new EndpointName("SendingACommandViaAProxy.Proxy");
                    c.Routing().UnicastRoutingTable.AddStatic(typeof(Request), receiverEndpoint, new EndpointInstanceName(proxyEndpoint, null, null));
                    c.Routing().EndpointInstances.AddStatic(receiverEndpoint, new EndpointInstanceName(receiverEndpoint, null, null));
                });
            }


            public class ResponseHandler : IHandleMessages<Response>
            {
                public Context Context { get; set; }

                public Task Handle(Response message)
                {
                    Context.SenderGotResponse = true;
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
                });
            }

            public class ReadyNotification : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public Task StartAsync()
                {
                    Context.ReceiverReady = true;
                    return Task.FromResult(0);
                }

                public Task StopAsync()
                {
                    return Task.FromResult(0);
                }
            }

            public class MyMessageHandler : IHandleMessages<Request>
            {
                public IBus Bus { get; set; }
                public Context Context { get; set; }

                public Task Handle(Request message)
                {
                    Context.ReceiverGotRequest = true;
                    return Bus.ReplyAsync(new Response());
                }
            }
        }

        public class Proxy : EndpointConfigurationBuilder
        {
            public Proxy()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Pipeline.Register("Proxy", typeof(ProxyBehavior), "Proxy");
                });
            }

            public class ReadyNotification : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public Task StartAsync()
                {
                    Context.ProxyReady = true;
                    return Task.FromResult(0);
                }

                public Task StopAsync()
                {
                    return Task.FromResult(0);
                }
            }

            public class ProxyBehavior : Behavior<TransportReceiveContext>
            {
                public Context Context { get; set; }
                public IDispatchMessages Dispatcher { get; set; }
                
                public override async Task Invoke(TransportReceiveContext context, Func<Task> next)
                {
                    var itinerary = Itinerary.ExtractFrom(context.Message.Headers);
                    string destination;
                    itinerary.Advance(out destination);
                    var outgoingMessage = new OutgoingMessage(context.Message.MessageId, context.Message.Headers, context.Message.Body);
                    var addressLabel = new UnicastAddressTag(destination);
                    await Dispatcher.Dispatch(new[]
                    {
                        new TransportOperation(outgoingMessage, new DispatchOptions(addressLabel, DispatchConsistency.Default))
                    }, context);
                    await next().ConfigureAwait(false);
                }
            }

            public class MessageDetector : IHandleMessages<IMessage>
            {
                public Context Context { get; set; }


                public Task Handle(IMessage message)
                {
                    Context.ProxyGotRequest |= message is Request;
                    Context.ProxyGotResponse |= message is Response;
                    return Task.FromResult(0);
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
