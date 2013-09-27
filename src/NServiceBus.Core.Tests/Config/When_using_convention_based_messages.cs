namespace NServiceBus.Core.Tests.Config
{
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class When_using_convention_based_messages
    {
        [Test]
        public void Should_include_messages()
        {
            Configure.With(new[] { typeof(x64.ConventionBasedHandler) });

            var typesToScan = Configure.TypesToScan;

            var foundType = typesToScan.FirstOrDefault(type => type.FullName == "ConventionBasedMessages.MyMessage");

            Assert.NotNull(foundType);
        }
    }
}