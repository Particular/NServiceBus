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
    }
}