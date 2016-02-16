namespace NServiceBus.Core.Tests.Correlation
{
    using NUnit.Framework;

    [TestFixture]
    public class CorrelationContextExtensionsTests
    {
        [Test]
        public void SendOptions_GetCorrelationId_Should_Return_Configured_CorrelationId()
        {
            const string expectedCorrelationId = "custom correlation id";
            var options = new SendOptions();
            options.SetCorrelationId(expectedCorrelationId);

            var correlationId = options.GetCorrelationId();

            Assert.AreEqual(expectedCorrelationId, correlationId);
        }

        [Test]
        public void SendOptions_GetCorrelationId_Should_Return_Null_When_No_CorrelationId_Configured()
        {
            var options = new SendOptions();

            var correlationId = options.GetCorrelationId();

            Assert.IsNull(correlationId);
        }

        [Test]
        public void ReplyOptions_GetCorrelationId_Should_Return_Configured_CorrelationId()
        {
            const string expectedCorrelationId = "custom correlation id";
            var options = new ReplyOptions();
            options.SetCorrelationId(expectedCorrelationId);

            var correlationId = options.GetCorrelationId();

            Assert.AreEqual(expectedCorrelationId, correlationId);
        }

        [Test]
        public void ReplyOptions_GetCorrelationId_Should_Return_Null_When_No_CorrelationId_Configured()
        {
            var options = new ReplyOptions();

            var correlationId = options.GetCorrelationId();

            Assert.IsNull(correlationId);
        }
    }
}