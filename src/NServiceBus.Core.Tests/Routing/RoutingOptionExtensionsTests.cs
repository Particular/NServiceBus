﻿namespace NServiceBus.Core.Tests.Routing
{
    using NUnit.Framework;

    [TestFixture]
    public class RoutingOptionExtensionsTests
    {
        [Test]
        public void SendOptions_IsRoutingReplyToThisInstance_Should_Return_True_When_Routing_Reply_To_This_Instance()
        {
            var options = new SendOptions();

            options.RouteReplyToThisInstance();

            Assert.IsTrue(options.IsRoutingReplyToThisInstance());
        }

        [Test]
        public void SendOptions_IsRoutingReplyToThisInstance_Should_Return_False_When_Not_Routing_Reply_To_This_Instance()
        {
            var options = new SendOptions();

            Assert.IsFalse(options.IsRoutingReplyToThisInstance());
        }

        [Test]
        public void ReplyOptions_IsRoutingReplyToThisInstance_Should_Return_True_When_Routing_Reply_To_This_Instance()
        {
            var options = new ReplyOptions();

            options.RouteReplyToThisInstance();

            Assert.IsTrue(options.IsRoutingReplyToThisInstance());
        }

        [Test]
        public void ReplyOptions_IsRoutingReplyToThisInstance_Should_Return_False_When_Not_Routing_Reply_To_This_Instance()
        {
            var options = new ReplyOptions();

            Assert.IsFalse(options.IsRoutingReplyToThisInstance());
        }

        [Test]
        public void SendOptions_IsRoutingReplyToAnyInstance_Should_Return_True_When_Routing_Reply_To_Any_Instance()
        {
            var options = new SendOptions();

            options.RouteReplyToAnyInstance();

            Assert.IsTrue(options.IsRoutingReplyToAnyInstance());
        }

        [Test]
        public void SendOptions_IsRoutingReplyToAnyInstance_Should_Return_False_When_Not_Routing_Reply_To_Any_Instance()
        {
            var options = new SendOptions();

            Assert.IsFalse(options.IsRoutingReplyToAnyInstance());
        }

        [Test]
        public void ReplyOptions_IsRoutingReplyToAnyInstance_Should_Return_True_When_Routing_Reply_To_Any_Instance()
        {
            var options = new ReplyOptions();

            options.RouteReplyToAnyInstance();

            Assert.IsTrue(options.IsRoutingReplyToAnyInstance());
        }

        [Test]
        public void ReplyOptions_IsRoutingReplyToAnyInstance_Should_Return_False_When_Not_Routing_Reply_To_Any_Instance()
        {
            var options = new ReplyOptions();

            Assert.IsFalse(options.IsRoutingReplyToAnyInstance());
        }

        [Test]
        public void ReplyOptions_GetReplyToRoute_Should_Return_Configured_Reply_Route()
        {
            const string expectedReplyToAddress = "custom replyTo address";
            var options = new ReplyOptions();

            options.RouteReplyTo(expectedReplyToAddress);

            Assert.AreEqual(expectedReplyToAddress, options.GetReplyToRoute());
        }

        [Test]
        public void ReplyOptions_GetReplyToRoute_Should_Return_Null_When_No_Route_Configured()
        {
            var options = new ReplyOptions();

            Assert.IsNull(options.GetReplyToRoute());
        }

        [Test]
        public void SendOptions_GetReplyToRoute_Should_Return_Configured_Reply_Route()
        {
            const string expectedReplyToAddress = "custom replyTo address";
            var options = new SendOptions();

            options.RouteReplyTo(expectedReplyToAddress);

            Assert.AreEqual(expectedReplyToAddress, options.GetReplyToRoute());
        }

        [Test]
        public void SendOptions_GetReplyToRoute_Should_Return_Null_When_No_Route_Configured()
        {
            var options = new SendOptions();

            Assert.IsNull(options.GetReplyToRoute());
        }
    }
}