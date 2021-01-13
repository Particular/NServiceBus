namespace NServiceBus.Core.Tests.Routing
{
    using NUnit.Framework;

    [TestFixture]
    public class RoutingOptionExtensionsTests
    {
        [Test]
        public void ReplyOptions_GetDestination_Should_Return_Configured_Destination()
        {
            const string expectedDestination = "custom reply destination";
            var options = new ReplyOptions();
            options.SetDestination(expectedDestination);

            var destination = options.GetDestination();

            Assert.AreEqual(expectedDestination, destination);
        }

        [Test]
        public void ReplyOptions_GetDestination_Should_Return_Null_When_No_Destination_Configured()
        {
            var options = new ReplyOptions();

            var destination = options.GetDestination();

            Assert.IsNull(destination);
        }

        [Test]
        public void SendOptions_GetDestination_Should_Return_Configured_Destination()
        {
            const string expectedDestination = "custom send destination";
            var options = new SendOptions();
            options.SetDestination(expectedDestination);

            var destination = options.GetDestination();

            Assert.AreEqual(expectedDestination, destination);
        }

        [Test]
        public void SendOptions_GetDestination_Should_Return_Null_When_No_Destination_Configured()
        {
            var options = new SendOptions();

            var destination = options.GetDestination();

            Assert.IsNull(destination);
        }

        [Test]
        public void IsRoutingToThisEndpoint_Should_Return_False_When_Not_Routed_To_This_Endpoint()
        {
            var options = new SendOptions();

            Assert.IsFalse(options.IsRoutingToThisEndpoint());
        }

        [Test]
        public void IsRoutingToThisEndpoint_Should_Return_True_When_Routed_To_This_Endpoint()
        {
            var options = new SendOptions();

            options.RouteToThisEndpoint();

            Assert.IsTrue(options.IsRoutingToThisEndpoint());
        }

        [Test]
        public void IsRoutingToThisInstance_Should_Return_False_When_Not_Routed_To_This_Instance()
        {
            var options = new SendOptions();

            Assert.IsFalse(options.IsRoutingToThisInstance());
        }

        [Test]
        public void IsRoutingToThisInstance_Should_Return_True_When_Routed_To_This_Instance()
        {
            var options = new SendOptions();

            options.RouteToThisInstance();

            Assert.IsTrue(options.IsRoutingToThisInstance());
        }

        [Test]
        public void GetRouteToSpecificInstance_Should_Return_Configured_Instance()
        {
            const string expectedInstanceId = "custom instance id";
            var options = new SendOptions();

            options.RouteToSpecificInstance(expectedInstanceId);

            Assert.AreEqual(expectedInstanceId, options.GetRouteToSpecificInstance());
        }

        [Test]
        public void GetRouteToSpecificInstance_Should_Return_Null_When_No_Instance_Configured()
        {
            var options = new SendOptions();

            Assert.IsNull(options.GetRouteToSpecificInstance());
        }

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