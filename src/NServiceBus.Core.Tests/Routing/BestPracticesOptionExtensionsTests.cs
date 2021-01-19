namespace NServiceBus.Core.Tests.Routing
{
    using NUnit.Framework;

    [TestFixture]
    public class BestPracticesOptionExtensionsTests
    {
        [Test]
        public void IgnoredBestPractices_Should_Return_False_When_Not_Disabled_Best_Practice_Enforcement()
        {
            var options = new SendOptions();

            Assert.IsFalse(options.IgnoredBestPractices());
        }

        [Test]
        public void IgnoredBestPractices_Should_Return_True_When_Disabled_Best_Practice_Enforcement()
        {
            var options = new PublishOptions();

            options.DoNotEnforceBestPractices();

            Assert.IsTrue(options.IgnoredBestPractices());
        }
    }
}