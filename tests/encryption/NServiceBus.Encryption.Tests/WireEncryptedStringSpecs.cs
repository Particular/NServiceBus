namespace NServiceBus.Encryption.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Config;
    using NUnit.Framework;

    [TestFixture]
    public class When_sending_a_message_using_the_default_convention : WireEncryptedStringContext
    {
        [Test]
        public void Should_use_the_wireencrypted_string()
        {
            var message = new Customer
                              {
                                  Secret="A secret",
                                  SecretField = "A secret",
                                  CreditCard = new CreditCardDetails{CreditCardNumber = "A secret"},
                                  ListOfCreditCards = new List<CreditCardDetails> { new CreditCardDetails { CreditCardNumber = "A secret" }, new CreditCardDetails { CreditCardNumber = "A secret" } } 
                              };
            message.ListOfSecrets = new ArrayList(message.ListOfCreditCards);

            mutator.MutateOutgoing(message);

            Assert.AreEqual(message.Secret.EncryptedValue.EncryptedBase64Value, EncryptedBase64Value);
            Assert.AreEqual(message.SecretField.EncryptedValue.EncryptedBase64Value, EncryptedBase64Value);
            Assert.AreEqual(message.CreditCard.CreditCardNumber.EncryptedValue.EncryptedBase64Value, EncryptedBase64Value);
            Assert.AreEqual(message.ListOfCreditCards[0].CreditCardNumber.EncryptedBase64Value, EncryptedBase64Value);
            Assert.AreEqual(message.ListOfCreditCards[1].CreditCardNumber.EncryptedBase64Value, EncryptedBase64Value);

            Assert.AreEqual(((CreditCardDetails)message.ListOfSecrets[0]).CreditCardNumber.EncryptedBase64Value, EncryptedBase64Value);
            Assert.AreEqual(((CreditCardDetails)message.ListOfSecrets[1]).CreditCardNumber.EncryptedBase64Value, EncryptedBase64Value);

        }
    }

    [TestFixture]
    public class When_encrypting_a_message_with_circular_references : WireEncryptedStringContext
    {
        [Test]
        public void Should_encrypt_the_property_correctly()
        {
            var child = new SubProperty {Secret = "A secret"};

            var message = new MessageWithCircularReferences
            {
               Child = child
            };
            child.Self = child;
            child.Parent = message;

            mutator.MutateOutgoing(message);

            Assert.AreEqual(message.Child.Secret.EncryptedValue.EncryptedBase64Value, EncryptedBase64Value);
            

        }
    }

    [TestFixture]
    public class When_decrypting_a_message_with_circular_references : WireEncryptedStringContext
    {
        [Test]
        public void Should_decrypt_the_property_correctly()
        {
            var child = new SubProperty { Secret = FakeEncryptedString};

            var message = new MessageWithCircularReferences
            {
                Child = child
            };
            child.Self = child;
            child.Parent = message;

            mutator.MutateIncoming(message);

            Assert.AreEqual(message.Child.Secret.Value, "A secret");


        }
    }

    [TestFixture]
    public class When_decrypting_a_member_that_is_missing_encryption_data : WireEncryptedStringContext
    {
        [Test]
        public void Should_throw_an_exception()
        {

            var message = new MessageWithMissingData
                              {
                                  Secret = new WireEncryptedString {Value = "The real value"}
                              };

            Assert.Throws<InvalidOperationException>(() => mutator.MutateIncoming(message));
        }
    }

    [TestFixture]
    public class When_receiving_a_message_with_a_encrypted_property_that_has_a_nonpublic_setter : WireEncryptedStringContext
    {
        [Test]
        public void Should_decrypt_correctly()
        {
            var message = new SecureMessageWithProtectedSetter(FakeEncryptedString);
            mutator.MutateIncoming(message);

            Assert.AreEqual(message.Secret.Value, "A secret");
        }
    }

    [TestFixture]
    public class When_sending_a_message_with_2x_compatibility_disabled : WireEncryptedStringContext
    {
        [Test]
        public void Should_clear_the_compatibility_properties()
        {
            ConfigureEncryption.DisableCompatibilityWithNSB2(null);

            var message = new Customer
            {
                Secret = "A secret"
            };
            mutator.MutateOutgoing(message);

            Assert.AreEqual(message.Secret.EncryptedValue.EncryptedBase64Value, EncryptedBase64Value);
            Assert.AreEqual(message.Secret.EncryptedBase64Value,null);
            Assert.AreEqual(message.Secret.Base64Iv, null);
        }
    }

    [TestFixture]
    public class When_decrypting_a_message_using_the_default_convention : WireEncryptedStringContext
    {
        [Test]
        public void Should_use_the_wireencrypted_string()
        {
            var message = new Customer
            {
                Secret = FakeEncryptedString,
                SecretField = FakeEncryptedString,
                CreditCard = new CreditCardDetails{CreditCardNumber = FakeEncryptedString}

            };
            mutator.MutateIncoming(message);

            Assert.AreEqual(message.Secret.Value, "A secret");
            Assert.AreEqual(message.SecretField.Value, "A secret");
            Assert.AreEqual(message.CreditCard.CreditCardNumber.Value, "A secret");
        }
    }

    [TestFixture]
    public class When_sending_a_encrypted_message_without_a_encryption_service_configured : WireEncryptedStringContext
    {
        [Test]
        public void Should_throw_an_exception()
        {
            mutator.EncryptionService = null;
            Assert.Throws<InvalidOperationException>(()=>mutator.MutateOutgoing(new Customer{Secret ="x"}));
        }
    }

    [TestFixture]
    public class When_receiving_a_encrypted_message_without_a_encryption_service_configured : WireEncryptedStringContext
    {
        [Test]
        public void Should_throw_an_exception()
        {
            mutator.EncryptionService = null;
            Assert.Throws<InvalidOperationException>(() => mutator.MutateIncoming(new Customer{Secret = "x"}));
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

        public const string EncryptedBase64Value = "encrypted value";

        public EncryptedValue FakeEncryptedValue;

        public WireEncryptedString FakeEncryptedString;

        [SetUp]
        public void BaseSetUp()
        {
            FakeEncryptedValue = new EncryptedValue
                                                              {
                                                                  EncryptedBase64Value = EncryptedBase64Value,
                                                                  Base64Iv = "init_vector"
                                                              };

            FakeEncryptedString = new WireEncryptedString { EncryptedValue = FakeEncryptedValue };
            mutator = new EncryptionMessageMutator
            {
                EncryptionService = new FakeEncryptionService(FakeEncryptedValue)
            };

            MessageConventionExtensions.IsEncryptedPropertyAction = property => typeof(WireEncryptedString).IsAssignableFrom(property.PropertyType);
        }
    }

    public class NonSecureMessage : IMessage
    {
        public string NotASecret { get; set; }
    }
    public class Customer:IMessage
    {
        public WireEncryptedString Secret { get; set; }
        public WireEncryptedString SecretField;
        public CreditCardDetails CreditCard { get; set; }

        public WireEncryptedString SecretThatIsNull { get; set; }

        public DateTime DateTime { get; set; }

        public List<CreditCardDetails> ListOfCreditCards { get; set; }

        public ArrayList ListOfSecrets {get; set; }
    }


    public class CreditCardDetails
    {
        public WireEncryptedString CreditCardNumber { get; set; }
    }


    public class SecureMessageWithProtectedSetter:IMessage
    {
        public SecureMessageWithProtectedSetter(WireEncryptedString secret)
        {
            Secret = secret;
        }
        public WireEncryptedString Secret { get; protected set; }
        
    }


    public class MessageWithCircularReferences : IMessage
    {
        public SubProperty Child { get; set; }

    }

    public class MessageWithMissingData: IMessage
    {
        public WireEncryptedString Secret { get; set; }

    }

    public class SubProperty
    {
        public MessageWithCircularReferences Parent { get; set; }
        public WireEncryptedString Secret { get; set; }
        public SubProperty Self { get; set; }
    }
}
