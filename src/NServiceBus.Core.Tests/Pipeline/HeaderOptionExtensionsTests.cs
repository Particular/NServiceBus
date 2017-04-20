namespace NServiceBus.Core.Tests.Pipeline
{
    using NUnit.Framework;

    [TestFixture]
    public class HeaderOptionExtensionsTests
    {
        [Test]
        public void GetHeaders_Should_Return_Configured_Headers()
        {
            var options = new SendOptions();
            options.SetHeader("custom header key 1", "custom header value 1");
            options.SetHeader("custom header key 2", "custom header value 2");

            var result = options.GetHeaders();

            Assert.AreEqual(2, result.Count);
            CollectionAssert.Contains(result.Values, "custom header value 1");
            CollectionAssert.Contains(result.Values, "custom header value 2");
        }
    }
}