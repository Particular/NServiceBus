namespace NServiceBus.Core.Tests.Encryption
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class WireEncryptedStringTests
    {
        [Test]
        public void Should_throw_an_exception()
        {
            var svc = new FakeEncryptionService(new EncryptedValue
            {
                EncryptedBase64Value = "EncryptedBase64Value",
                Base64Iv = "Base64Iv"
            });

            var value = new WireEncryptedString
            {
                Value = "The real value"
            };

            // ReSharper disable once InvokeAsExtensionMethod
            var exception = Assert.Throws<Exception>(() => WireEncryptedStringConversions.DecryptValue(svc, value, null));
            Assert.AreEqual("Encrypted property is missing encryption data", exception.Message);
        }
    }
}