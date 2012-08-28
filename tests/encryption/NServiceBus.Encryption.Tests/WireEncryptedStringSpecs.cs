namespace NServiceBus.Encryption.Tests
{
    using System;
    using Config;
    using NUnit.Framework;

    [TestFixture]
    public class When_sending_a_message_using_the_default_convention : WireEncryptedStringContext
    {
        [Test]
        public void Should_use_the_wireencrypted_string()
        {
            var message = new SecureMessage
                              {
                                  Secret="A secret",
                                  SubProperty = new MySecretSubProperty{Secret = "A secret"}
                                  
                              };
            mutator.MutateOutgoing(message);

            Assert.AreEqual(message.Secret.EncryptedValue.EncryptedBase64Value,"encrypted value");
            Assert.AreEqual(message.SubProperty.Secret.EncryptedValue.EncryptedBase64Value, "encrypted value");
        }
    }

    [TestFixture]
    public class When_sending_a_message_with_2x_compatibility_disabled : WireEncryptedStringContext
    {
        [Test]
        public void Should_clear_the_compatibility_properties()
        {
            ConfigureEncryption.DisableCompatibilityWithNSB2(null);

            var message = new SecureMessage
            {
                Secret = "A secret"
            };
            mutator.MutateOutgoing(message);

            Assert.AreEqual(message.Secret.EncryptedValue.EncryptedBase64Value, "encrypted value");
            Assert.AreEqual(message.Secret.EncryptedBase64Value,null);
            Assert.AreEqual(message.Secret.Base64Iv, null);
        }
    }

    [TestFixture]
    public class When_receiving_a_message_using_the_default_convention : WireEncryptedStringContext
    {
        [Test]
        public void Should_use_the_wireencrypted_string()
        {
            var message = new SecureMessage
            {
                Secret = new WireEncryptedString
                             {
                                 EncryptedValue = new EncryptedValue
                                                      {
                                                          EncryptedBase64Value ="encrypted_value",
                                                          Base64Iv = "init_vector"
                                                      }
                             },
                SubProperty = new MySecretSubProperty
                {
                    Secret = new WireEncryptedString
                    {
                        EncryptedValue = new EncryptedValue
                        {
                            EncryptedBase64Value = "encrypted_value",
                            Base64Iv = "init_vector"
                        }
                    }
                }

            };
            mutator.MutateIncoming(message);

            Assert.AreEqual(message.Secret.Value, "A secret");
            Assert.AreEqual(message.SubProperty.Secret.Value, "A secret");
        }
    }

    [TestFixture]
    public class When_sending_a_encrypted_message_without_a_encryption_service_configured : WireEncryptedStringContext
    {
        [Test]
        public void Should_throw_an_exception()
        {
            mutator.EncryptionService = null;
            Assert.Throws<InvalidOperationException>(()=>mutator.MutateOutgoing(new SecureMessage{Secret ="x"}));
        }
    }

    [TestFixture]
    public class When_receiving_a_encrypted_message_without_a_encryption_service_configured : WireEncryptedStringContext
    {
        [Test]
        public void Should_throw_an_exception()
        {
            mutator.EncryptionService = null;
            Assert.Throws<InvalidOperationException>(() => mutator.MutateIncoming(new SecureMessage{Secret = "x"}));
        }
    }

    [TestFixture]
    public class When_sending_a_non_encrypted_message_without_a_encryption_service_configured:WireEncryptedStringContext
    {
        [Test]
        public void Should_not_throw_an_exception()
        {
            mutator.EncryptionService = null;
         
            Assert.DoesNotThrow(() => mutator.MutateOutgoing(new NonSecureMessage()));
        }
    }

    [TestFixture]
    public class When_receiving_a_non_encrypted_message_without_a_encryption_service_configured : WireEncryptedStringContext
    {
        [Test]
        public void Should_not_throw_an_exception()
        {
            mutator.EncryptionService = null;

            Assert.DoesNotThrow(() => mutator.MutateIncoming(new NonSecureMessage()));
        }
    }


    public class WireEncryptedStringContext
    {
        protected EncryptionMessageMutator mutator;

        [SetUp]
        public void SetUp()
        {
            mutator = new EncryptionMessageMutator
            {
                EncryptionService = new FakeEncryptionService("encrypted value")
            };

            MessageConventionExtensions.IsEncryptedPropertyAction = property => typeof(WireEncryptedString).IsAssignableFrom(property.PropertyType);
        }
    }

    public class NonSecureMessage : IMessage
    {
        public string NotASecret { get; set; }
    }
    public class SecureMessage:IMessage
    {
        public WireEncryptedString Secret { get; set; }

        public MySecretSubProperty SubProperty { get; set; }

        public WireEncryptedString SecretThatIsNull { get; set; }

        public DateTime DateTime { get; set; }
    }


    public class MySecretSubProperty
    {
        public WireEncryptedString Secret { get; set; }
    }
}
