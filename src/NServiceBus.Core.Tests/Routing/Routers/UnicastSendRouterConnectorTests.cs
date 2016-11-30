namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using Unicast.Messages;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class UnicastSendRouterConnectorTests
    {
        [Test]
        public async Task Should_set_messageintent_to_send()
        {
            var behavior = InitializeBehavior();
            var options = new SendOptions();
            options.SetDestination("destination endpoint");
            var context = CreateContext(options);

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual(1, context.Headers.Count);
            Assert.AreEqual(MessageIntentEnum.Send.ToString(), context.Headers[Headers.MessageIntent]);
        }

        [Test]
        public async Task Should_use_explicit_route_for_sends_if_present()
        {
            var behavior = InitializeBehavior();
            var options = new SendOptions();

            options.SetDestination("destination endpoint");

            var context = CreateContext(options);

            UnicastRoute route = null;
            await behavior.Invoke(context, c =>
            {
                route = c.Destinations.Single();
                return TaskEx.CompletedTask;
            });

            Assert.AreEqual("destination endpoint", route.PhysicalAddress);
        }

        [Test]
        public async Task Should_route_to_local_endpoint_if_requested_so()
        {
            var behavior = InitializeBehavior(sharedQueue: "MyLocalAddress");
            var options = new SendOptions();

            options.RouteToThisEndpoint();

            var context = CreateContext(options);

            UnicastRoute route = null;
            await behavior.Invoke(context, c =>
            {
                route = c.Destinations.Single();
                return TaskEx.CompletedTask;
            });

            Assert.AreEqual("MyLocalAddress", route.PhysicalAddress);
        }

        [Test]
        public async Task Should_route_to_local_instance_if_requested_so()
        {
            var behavior = InitializeBehavior(sharedQueue: "MyLocalAddress", instanceSpecificQueue: "MyInstance");
            var options = new SendOptions();

            options.RouteToThisInstance();

            var context = CreateContext(options);

            UnicastRoute route = null;
            await behavior.Invoke(context, c =>
            {
                route = c.Destinations.Single();
                return TaskEx.CompletedTask;
            });

            Assert.AreEqual("MyInstance", route.PhysicalAddress);
        }

        [Test]
        public async Task Should_throw_if_requested_to_route_to_local_instance_and_instance_has_no_specific_queue()
        {
            var behavior = InitializeBehavior(sharedQueue: "MyLocalAddress", instanceSpecificQueue: null);

            try
            {
                var options = new SendOptions();

                options.RouteToThisInstance();

                var context = CreateContext(options);
                await behavior.Invoke(context, c => TaskEx.CompletedTask);
                Assert.Fail("RouteToThisInstance");
            }
            catch (Exception)
            {
                // ignored
            }
        }

        [Test]
        public async Task Should_throw_if_invalid_route_combinations_are_used()
        {
            var behavior = InitializeBehavior(sharedQueue: "MyLocalAddress", instanceSpecificQueue: "MyInstance");

            try
            {
                var options = new SendOptions();

                options.RouteToThisInstance();
                options.SetDestination("Destination");

                var context = CreateContext(options);
                await behavior.Invoke(context, c => TaskEx.CompletedTask);
                Assert.Fail("RouteToThisInstance+SetDestination");
            }
            catch (Exception)
            {
                // ignored
            }

            try
            {
                var options = new SendOptions();

                options.RouteToThisEndpoint();
                options.SetDestination("Destination");

                var context = CreateContext(options);
                await behavior.Invoke(context, c => TaskEx.CompletedTask);
                Assert.Fail("RouteToThisEndpoint+SetDestination");
            }
            catch (Exception)
            {
                // ignored
            }

            try
            {
                var options = new SendOptions();

                options.RouteToThisEndpoint();
                options.RouteToThisInstance();

                var context = CreateContext(options);
                await behavior.Invoke(context, c => TaskEx.CompletedTask);
                Assert.Fail("RouteToThisEndpoint+RouteToThisInstance");
            }
            catch (Exception)
            {
                // ignored
            }
        }

        [Test]
        public async Task Should_route_using_the_mappings_if_no_destination_is_set()
        {
            var context = CreateContext(new SendOptions());

            var routingTable = new UnicastRoutingTable();
            routingTable.AddOrReplaceRoutes("key", new List<RouteTableEntry>()
            {
                new RouteTableEntry(context.Message.MessageType, UnicastRoute.CreateFromEndpointName("Destination"))
            });

            var behavior = InitializeBehavior(routingTable: routingTable);

            UnicastRoute route = null;
            await behavior.Invoke(context, c =>
            {
                route = c.Destinations.Single();
                return TaskEx.CompletedTask;
            });

            Assert.AreEqual("Destination", route.Endpoint);
        }

        [Test]
        public void Should_throw_if_no_route_can_be_found()
        {
            var behavior = InitializeBehavior();

            var context = CreateContext(new SendOptions());

            Assert.That(async () => await behavior.Invoke(context, _ => TaskEx.CompletedTask), Throws.InstanceOf<Exception>().And.Message.Contains("No destination specified"));
        }

        static IOutgoingSendContext CreateContext(SendOptions options)
        {
            var message = new MyMessage();
            var context = new TestableOutgoingSendContext
            {
                Message = new OutgoingLogicalMessage(message.GetType(), message),
                Extensions = options.Context
            };
            return context;
        }


        static UnicastSendRouterConnector InitializeBehavior(
            string sharedQueue = null,
            string instanceSpecificQueue = null,
            UnicastRoutingTable routingTable = null)
        {
            var metadataRegistry = new MessageMetadataRegistry(new Conventions());
            metadataRegistry.RegisterMessageTypesFoundIn(new List<Type> { typeof(MyMessage) });

            return new UnicastSendRouterConnector(sharedQueue, instanceSpecificQueue, routingTable ?? new UnicastRoutingTable());
        }

        class MyMessage
        {
        }
    }
}