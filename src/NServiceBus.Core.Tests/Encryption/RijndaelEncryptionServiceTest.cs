namespace NServiceBus.Core.Tests.Encryption
{
    using NServiceBus.Encryption.Rijndael;
    using NUnit.Framework;

    [TestFixture]
    public class RijndaelEncryptionServiceTest
    {
        [Test]
        public void Should_encrypt_and_decrypt()
        {
            var rijndaelEncryptionService = new RijndaelEncryptionService("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
            var encryptedValue = rijndaelEncryptionService.Encrypt("string to encrypt");
            Assert.AreNotEqual("string to encrypt", encryptedValue.EncryptedBase64Value);
            var decryptedValue = rijndaelEncryptionService.Decrypt(encryptedValue);
            Assert.AreEqual("string to encrypt", decryptedValue);
        }
    }

}