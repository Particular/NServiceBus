namespace NServiceBus.Core.Tests.Licensing
{
    using NUnit.Framework;
    using Particular.Licensing;

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