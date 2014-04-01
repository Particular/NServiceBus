namespace NServiceBus.Core.Tests.Licensing
{
    using System;
    using NServiceBus.Licensing;
    using NUnit.Framework;

    [TestFixture]
    public class LicenseExpiredFormDisplayerTests
    {

        [Test]
        [Explicit]
        public void ShowForm14Days()
        {
            LicenseExpiredFormDisplayer.PromptUserForLicense(Particular.Licensing.License.TrialLicense(DateTime.Today.AddDays(33)));
        }


        [Test]
        [Explicit]
        public void ShowForm45Days()
        {
            LicenseExpiredFormDisplayer.PromptUserForLicense(null);
        }
    }
}