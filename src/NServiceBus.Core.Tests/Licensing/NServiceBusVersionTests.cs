namespace NServiceBus.Core.Tests.Licensing
{
    using System;
    using NServiceBus.Licensing;
    using NUnit.Framework;

    [TestFixture]
    public class NServiceBusVersionTests
    {

        [Test]
        public void Verify_has_a_version_and_van_be_parsed()
        {
            Assert.IsNotNullOrEmpty(NServiceBusVersion.MajorAndMinor);
            Version version;
            Assert.IsTrue(Version.TryParse(NServiceBusVersion.MajorAndMinor, out version));
        }
    }
}