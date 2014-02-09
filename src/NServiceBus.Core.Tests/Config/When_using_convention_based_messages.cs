namespace NServiceBus.Core.Tests.Config
{
    using NUnit.Framework;

    [TestFixture]
    public class When_using_convention_based_messages
    {
        [Test]
        [Explicit("//TODO: re-enable when we make message scanning lazy #1617")]
        public void Should_include_messages_of_a_handler()
        {
            //Configure.With(new[] { typeof(ConventionBasedHandler) });

            //var typesToScan = Configure.TypesToScan;

            //var foundType = typesToScan.FirstOrDefault(type => type.FullName == "ConventionBasedMessages.MyMessage");

            //Assert.NotNull(foundType);
        }
    }
}