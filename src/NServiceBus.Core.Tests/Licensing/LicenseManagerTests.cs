namespace NServiceBus.Core.Tests.Licensing
{
    using System.Configuration;
    using System.IO;
    using System.Reflection;
    using NServiceBus.Licensing;
    using NUnit.Framework;

    [TestFixture]
    public class LicenseManagerTests
    {

        [Test]
        public void License_for_future_UpgradeProtection_does_not_throw()
        {
            var validator = LicenseManager.CreateValidator(ReadResourceAsString(@"Licensing.LicenseWithFutureUpgradeProtection.xml"));
            validator.TryLoadingLicenseValuesFromValidatedXml();
            LicenseManager.CheckIfUpgradeProtectionHasExpired(validator);
        }

        [Test]
        public void License_for_old_UpgradeProtection_throws()
        {
            var configurationErrorsException = Assert.Throws<ConfigurationErrorsException>(() =>
            {
                var validator = LicenseManager.CreateValidator(ReadResourceAsString(@"Licensing.LicenseWithOldUpgradeProtection.xml"));
                validator.TryLoadingLicenseValuesFromValidatedXml();
                LicenseManager.CheckIfUpgradeProtectionHasExpired(validator);
            });
            Assert.AreEqual("Your license upgrade protection does not cover this version of NServiceBus. You can renew it at http://particular.net/licensing", configurationErrorsException.Message);
        }

        string ReadResourceAsString(string path)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(assembly.GetName().Name +"."+ path))
            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
}