namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Testing;
    using Transport;

    [TestFixture]
    public class ApplyReplyToAddressBehaviorTests
    {
        [Test]
        public async Task Should_use_public_return_address_if_specified()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyEndpoint", "MyInstance", "PublicAddress", null);
            var options = new SendOptions();
            var context = CreateContext(options);

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual("PublicAddress", context.Headers[Headers.ReplyToAddress]);
        }

        static IOutgoingLogicalMessageContext CreateContext(ExtendableOptions options)
        {
            var context = new TestableOutgoingLogicalMessageContext
            {
                Extensions = options.Context
            };

            return context;
        }

        [Test]
        public async Task Should_default_to_setting_the_reply_to_header_to_this_endpoint()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyEndpoint", "MyInstance", null, null);
            var options = new SendOptions();
            var context = CreateContext(options);

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual("MyEndpoint", context.Headers[Headers.ReplyToAddress]);
        }

        [Test]
        public async Task Should_set_the_reply_to_header_to_this_endpoint_when_requested()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyEndpoint", "MyInstance", null, null);
            var options = new SendOptions();

            options.RouteReplyToAnyInstance();

            var context = CreateContext(options);
            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual("MyEndpoint", context.Headers[Headers.ReplyToAddress]);
        }

        [Test]
        public async Task Should_set_the_reply_to_header_to_this_instance_when_requested()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyEndpoint", "MyInstance", null, null);
            var options = new SendOptions();

            options.RouteReplyToThisInstance();

            var context = CreateContext(options);
            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual("MyInstance", context.Headers[Headers.ReplyToAddress]);
        }

        [Test]
        public async Task Should_set_the_reply_to_header_a_specified_address_when_requested()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyEndpoint", "MyInstance", null, null);
            var options = new SendOptions();

            options.RouteReplyTo("Destination");

            var context = CreateContext(options);
            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual("Destination", context.Headers[Headers.ReplyToAddress]);
        }

        [Test]
        public async Task Should_set_the_reply_to_distributor_address_when_message_comes_from_a_distributor()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyEndpoint", "MyInstance", "MyPublicAddress", "MyDistributor");
            var options = new SendOptions();
            var context = CreateContext(options);

            context.Extensions.Set(new IncomingMessage("ID", new Dictionary<string, string>
            {
                {LegacyDistributorHeaders.WorkerSessionId, "SessionID"}
            }, new byte[0]));

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual("MyDistributor", context.Headers[Headers.ReplyToAddress]);
        }

        [Test]
        public async Task Should_set_the_reply_to_user_overridden_local_endpoint_even_when_message_comes_from_a_distributor()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyEndpoint", "MyInstance", "MyPublicAddress", "MyDistributor");
            var options = new SendOptions();
            var context = CreateContext(options);

            context.Extensions.Set(new IncomingMessage("ID", new Dictionary<string, string>
            {
                {LegacyDistributorHeaders.WorkerSessionId, "SessionID"}
            }, new byte[0]));

            var state = context.Extensions.GetOrCreate<ApplyReplyToAddressBehavior.State>();
            state.Option = ApplyReplyToAddressBehavior.RouteOption.RouteReplyToAnyInstanceOfThisEndpoint;

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual("MyEndpoint", context.Headers[Headers.ReplyToAddress]);
        }

        [Test]
        public async Task Should_set_the_reply_to_user_overridden_local_instance_even_when_message_comes_from_a_distributor()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyEndpoint", "MyInstance", "MyPublicAddress", "MyDistributor");
            var options = new SendOptions();
            var context = CreateContext(options);

            context.Extensions.Set(new IncomingMessage("ID", new Dictionary<string, string>
            {
                {LegacyDistributorHeaders.WorkerSessionId, "SessionID"}
            }, new byte[0]));

            var state = context.Extensions.GetOrCreate<ApplyReplyToAddressBehavior.State>();
            state.Option = ApplyReplyToAddressBehavior.RouteOption.RouteReplyToThisInstance;

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual("MyInstance", context.Headers[Headers.ReplyToAddress]);
        }

        [Test]
        public async Task Should_throw_when_trying_to_route_replies_to_this_instance_when_no_instance_id_is_used()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyEndpoint", null, null, null);
            var options = new SendOptions();

            options.RouteReplyToThisInstance();

            var context = CreateContext(options);

            try
            {
                await behavior.Invoke(context, ctx => TaskEx.CompletedTask);
                Assert.Fail("Expected exception");
            }
            catch (Exception)
            {
                Assert.Pass();
            }
        }

        [Test]
        public async Task Should_throw_when_conflicting_settings_are_specified()
        {
            var behavior = new ApplyReplyToAddressBehavior("MyEndpoint", "MyInstance", null, null);

            try
            {
                var options = new SendOptions();
                var context = CreateContext(options);

                options.RouteReplyToAnyInstance();
                options.RouteReplyToThisInstance();

                await behavior.Invoke(context, ctx => TaskEx.CompletedTask);
                Assert.Fail("Expected exception");
            }
            catch (Exception)
            {
                Assert.Pass();
            }
        }
    }
}