namespace NServiceBus.Core.Tests.Encryption
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class ConfigureRijndaelEncryptionServiceTests
    {
        [Test]
        public void Test_invalid_keys()
        {
            var emptyKeyException = Assert.Throws<Exception>(() => ConfigureRijndaelEncryptionService.RijndaelEncryptionService(null, ""));
            Assert.AreEqual("The RijndaelEncryption key is empty. Please specify a valid 'RijndaelEncryptionServiceConfig' in your application's configuration file or pass in valid key to RijndaelEncryptionService.", emptyKeyException.Message);

            var whiteSpaceKeyException = Assert.Throws<Exception>(() => ConfigureRijndaelEncryptionService.RijndaelEncryptionService(null, " "));
            Assert.AreEqual("The RijndaelEncryption key is empty. Please specify a valid 'RijndaelEncryptionServiceConfig' in your application's configuration file or pass in valid key to RijndaelEncryptionService.", whiteSpaceKeyException.Message);

            var nullKeyException = Assert.Throws<Exception>(() => ConfigureRijndaelEncryptionService.RijndaelEncryptionService(null, null));
            Assert.AreEqual("The RijndaelEncryption key is empty. Please specify a valid 'RijndaelEncryptionServiceConfig' in your application's configuration file or pass in valid key to RijndaelEncryptionService.", nullKeyException.Message);

        }
    }
}