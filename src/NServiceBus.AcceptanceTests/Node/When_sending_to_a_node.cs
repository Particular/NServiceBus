namespace NServiceBus.AcceptanceTests.Node
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NUnit.Framework;

    public class When_sending_to_a_node: NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_the_message()
        {
            var nodeConfig = new MicroEndpointConfiguration(new LogicalAddress(new EndpointInstance("SendingToANode.Receiver", null, null)));
            nodeConfig.UseTransport<MsmqTransport>();
            var processor = new NodeProcessingBehavior();
            nodeConfig.Pipeline.Register("Processor", typeof(NodeProcessingBehavior), "Processor");
            nodeConfig.Container.ConfigureComponent(b => processor, DependencyLifecycle.SingleInstance);

            var node = await nodeConfig.Initialize().Start();

            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When((bus, c) => bus.Send(new MyMessage())))
                .Done(c => c.Done)
                .Run();
            
            Assert.AreEqual("Hello World!", ctx.MessageHeader);
            await node.Stop();
        }

        class NodeProcessingBehavior : Behavior<TransportReceiveContext>
        {
            public IDispatchMessages Dispatcher { get; set; }

            public override Task Invoke(TransportReceiveContext context, Func<Task> next)
            {
                var headers = new Dictionary<string, string>
                {
                    ["Message"] = "Hello World!",
                    [Headers.EnclosedMessageTypes] = typeof(MyMessage).AssemblyQualifiedName
                };
                var addressTag = new UnicastAddressTag(context.Message.Headers[Headers.OriginatingEndpoint]);
                var reply = new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), headers, context.Message.Body), new DispatchOptions(addressTag, DispatchConsistency.Default));
                return Dispatcher.Dispatch(new[] {reply}, context.Extensions);
            }
        }

        public class Context : ScenarioContext
        {
            public string MessageHeader { get; set; }
            public bool Done { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Routing().UnicastRoutingTable.RouteToEndpoint(typeof(MyMessage), "SendingToANode.Receiver");                    
                });
            }

            public class ReplyHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    Context.MessageHeader = context.MessageHeaders["Message"];
                    Context.Done = true;
                    return Task.FromResult(0);
                }
            }
        }
        

        public class MyMessage : IMessage
        {
        }
    }
}
