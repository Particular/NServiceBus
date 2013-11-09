namespace NServiceBus.Core.Tests.Licensing
{
    using NServiceBus.Licensing;
    using NUnit.Framework;

    [TestFixture]
    public class LicenseExpiredFormDisplayerTests
    {

        [Test]
        [Explicit]
        public void ShowForm()
        {
            LicenseExpiredFormDisplayer.PromptUserForLicense();
        }
    }
}