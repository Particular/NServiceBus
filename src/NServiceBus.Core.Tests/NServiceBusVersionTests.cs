namespace NServiceBus
{
    using NUnit.Framework;

    [TestFixture]
    public class NServiceBusVersionTests
    {
        [Test]
        public void Verify_has_a_version_and_can_be_parsed()
        {
            Assert.IsNotNullOrEmpty(NServiceBusVersion.Version);
        }
    }
}