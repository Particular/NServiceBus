namespace NServiceBus.Encryption.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class When_sending_a_message_using_the_default_convention
    {
        [Test]
        public void Should_use_the_wireencrypted_string()
        {
            var mutator = new EncryptionMessageMutator
                              {
                                  EncryptionService = new FakeEncryptionService("encrypted value")
                              };
            var message = new SecureMessage
                              {
                                  Secret="A secret"
                              };
            mutator.MutateOutgoing(message);

            Assert.AreEqual(message.Secret.EncryptedValue.EncryptedBase64Value,"encrypted value");
        }
    }

    [TestFixture]
    public class When_sending_a_encrypted_message_without_a_encryption_service_configured
    {
        [Test]
        public void Should_throw_an_exception()
        {
            var mutator = new EncryptionMessageMutator();

            Assert.Throws<InvalidOperationException>(()=>mutator.MutateOutgoing(new SecureMessage()));
        }
    }

    [TestFixture]
    public class When_sending_a_non_encrypted_message_without_a_encryption_service_configured
    {
        [Test]
        public void Should_not_throw_an_exception()
        {
            var mutator = new EncryptionMessageMutator();


            Assert.DoesNotThrow(() => mutator.MutateOutgoing(new NonSecureMessage()));
        }
    }

    public class NonSecureMessage : IMessage
    {
        public string NotASecret { get; set; }
    }
    public class SecureMessage:IMessage
    {
        public WireEncryptedString Secret { get; set; }
    }
}
