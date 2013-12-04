namespace NServiceBus.Core.Tests.Licensing
{
    using NServiceBus.Licensing;
    using NUnit.Framework;

    [TestFixture]
    public class UserSidCheckerTests
    {

        [Test]
        public void IsNotSystemSid_does_not_throw()
        {
            Assert.IsTrue(UserSidChecker.IsNotSystemSid());
        }
    }
}