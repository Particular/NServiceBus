namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class UnicastSendRouteOptionsTests
    {
        const string Message = "Already specified routing option for this message: {0}";

        [Test]
        public void RouteToThisInstance_With_SetDestination_should_throw()
        {
            var options = new SendOptions();
            options.RouteToThisInstance();

            var exception = Assert.Throws<Exception>(() => options.SetDestination("Destination"));

            Assert.That(exception.Message, Is.EqualTo(string.Format(Message, "RouteToThisInstance")));
        }

        [Test]
        public void RouteToThisInstance_With_RouteToSpecificInstance_should_throw()
        {
            var options = new SendOptions();
            options.RouteToThisInstance();

            var exception = Assert.Throws<Exception>(() => options.RouteToSpecificInstance("Id"));

            Assert.That(exception.Message, Is.EqualTo(string.Format(Message, "RouteToThisInstance")));
        }

        [Test]
        public void RouteToThisInstance_With_RouteToThisEndpoint_should_throw()
        {
            var options = new SendOptions();
            options.RouteToThisInstance();

            var exception = Assert.Throws<Exception>(() => options.RouteToThisEndpoint());

            Assert.That(exception.Message, Is.EqualTo(string.Format(Message, "RouteToThisInstance")));
        }

        [Test]
        public void RouteToThisEndpoint_With_SetDestination_should_throw()
        {
            var options = new SendOptions();
            options.RouteToThisEndpoint();

            var exception = Assert.Throws<Exception>(() => options.SetDestination("Destination"));

            Assert.That(exception.Message, Is.EqualTo(string.Format(Message, "RouteToAnyInstanceOfThisEndpoint")));
        }

        [Test]
        public void RouteToThisEndpoint_With_RouteToSpecificInstance_should_throw()
        {
            var options = new SendOptions();
            options.RouteToThisEndpoint();

            var exception = Assert.Throws<Exception>(() => options.RouteToSpecificInstance("Id"));

            Assert.That(exception.Message, Is.EqualTo(string.Format(Message, "RouteToAnyInstanceOfThisEndpoint")));
        }

        [Test]
        public void RouteToThisEndpoint_With_RouteToThisInstance_should_throw()
        {
            var options = new SendOptions();
            options.RouteToThisEndpoint();

            var exception = Assert.Throws<Exception>(() => options.RouteToThisInstance());

            Assert.That(exception.Message, Is.EqualTo(string.Format(Message, "RouteToAnyInstanceOfThisEndpoint")));
        }

        [Test]
        public void RouteToSpecificInstance_With_SetDestination_should_throw()
        {
            var options = new SendOptions();
            options.RouteToSpecificInstance("");

            var exception = Assert.Throws<Exception>(() => options.SetDestination("Destination"));

            Assert.That(exception.Message, Is.EqualTo(string.Format(Message, "RouteToSpecificInstance")));
        }

        [Test]
        public void RouteToSpecificInstance_With_RouteToToThisEndpoint_should_throw()
        {
            var options = new SendOptions();
            options.RouteToSpecificInstance("");

            var exception = Assert.Throws<Exception>(() => options.RouteToThisEndpoint());

            Assert.That(exception.Message, Is.EqualTo(string.Format(Message, "RouteToSpecificInstance")));
        }

        [Test]
        public void RouteToSpecificInstance_With_RouteToThisInstance_should_throw()
        {
            var options = new SendOptions();
            options.RouteToSpecificInstance("");

            var exception = Assert.Throws<Exception>(() => options.RouteToThisInstance());

            Assert.That(exception.Message, Is.EqualTo(string.Format(Message, "RouteToSpecificInstance")));
        }

        [Test]
        public void SetDestination_With_RouteToSpecificInstance_should_throw()
        {
            var options = new SendOptions();
            options.SetDestination("Destination");

            var exception = Assert.Throws<Exception>(() => options.RouteToSpecificInstance("Instance"));

            Assert.That(exception.Message, Is.EqualTo(string.Format(Message, "ExplicitDestination")));
        }

        [Test]
        public void SetDestination_With_RouteToThisEndpoint_should_throw()
        {
            var options = new SendOptions();
            options.SetDestination("Destination");

            var exception = Assert.Throws<Exception>(() => options.RouteToThisEndpoint());

            Assert.That(exception.Message, Is.EqualTo(string.Format(Message, "ExplicitDestination")));
        }

        [Test]
        public void SetDestination_With_RouteToThisInstance_should_throw()
        {
            var options = new SendOptions();
            options.SetDestination("Destination");

            var exception = Assert.Throws<Exception>(() => options.RouteToThisInstance());

            Assert.That(exception.Message, Is.EqualTo(string.Format(Message, "ExplicitDestination")));
        }
    }
}