namespace NServiceBus.Core.Tests.Encryption
{
    using NUnit.Framework;

    [TestFixture]
    public class When_sending_a_message_with_2x_compatibility_disabled : WireEncryptedStringContext
    {
        [Test]
        public void Should_clear_the_compatibility_properties()
        {
            var svc = new FakeEncryptionService(new EncryptedValue
            {
                EncryptedBase64Value = EncryptedBase64Value,
                Base64Iv = "Base64Iv"
            });

            var value = (WireEncryptedString) MySecretMessage;

            WireEncryptedStringConversions.EncryptValue(svc, value, null);
            Assert.AreEqual(value.EncryptedValue.EncryptedBase64Value, EncryptedBase64Value);
        }
    }
}