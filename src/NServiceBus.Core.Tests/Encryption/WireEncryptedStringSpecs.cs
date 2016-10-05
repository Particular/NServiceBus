namespace NServiceBus.Core.Tests.Encryption
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class When_inspecting_a_message_using_the_default_convention : WireEncryptedStringContext
    {
        [Test]
        public void Should_use_the_wireEncrypted_string()
        {
            var message = new Customer
                {
                    Secret = MySecretMessage,
                    SecretField = MySecretMessage,
                    CreditCard = new CreditCardDetails {CreditCardNumber = MySecretMessage},
                    LargeByteArray = new byte[1000000],
                    ListOfCreditCards =
                        new List<CreditCardDetails>
                            {
                                new CreditCardDetails {CreditCardNumber = MySecretMessage},
                                new CreditCardDetails {CreditCardNumber = MySecretMessage}
                            }
                };
            message.ListOfSecrets = new ArrayList(message.ListOfCreditCards);

            var result = inspector.ScanObject(message).ToList();
            result.ForEach(x => x.Item2.SetValue(x.Item1, Create()));

            Assert.AreEqual(5, result.Count);

            Assert.AreEqual(EncryptedBase64Value, message.Secret.EncryptedValue.EncryptedBase64Value);
            Assert.AreEqual(EncryptedBase64Value, message.SecretField.EncryptedValue.EncryptedBase64Value);
            Assert.AreEqual(EncryptedBase64Value, message.CreditCard.CreditCardNumber.EncryptedValue.EncryptedBase64Value);
            Assert.AreEqual(EncryptedBase64Value, message.ListOfCreditCards[0].CreditCardNumber.EncryptedValue.EncryptedBase64Value);
            Assert.AreEqual(EncryptedBase64Value, message.ListOfCreditCards[1].CreditCardNumber.EncryptedValue.EncryptedBase64Value);

            Assert.AreEqual(EncryptedBase64Value, ((CreditCardDetails)message.ListOfSecrets[0]).CreditCardNumber.EncryptedValue.EncryptedBase64Value);
            Assert.AreEqual(EncryptedBase64Value, ((CreditCardDetails)message.ListOfSecrets[1]).CreditCardNumber.EncryptedValue.EncryptedBase64Value);
        }
    }

    [TestFixture]
    public class When_inspecting_a_message_with_indexed_properties : WireEncryptedStringContext
    {
        [Test]
        public void Should_match_the_property_correctly()
        {
            var message = new MessageWithIndexedProperties
            {
                Secret = Create()
            };

            message[0] = "boo";
            message[1] = "foo";

            var result = inspector.ScanObject(message).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreSame(message.Secret, result[0].Item2.GetValue(result[0].Item1));
        }

        public class MessageWithIndexedProperties : IMessage
        {
            string[] indexedList = new string[2];

            public string this[int index]
            {
                get { return indexedList[index]; }
                set { indexedList[index] = value; }
            }

            public WireEncryptedString Secret { get; set; }
        }
    }

    [TestFixture]
    public class When_inspecting_a_message_with_WireEncryptedString_as_an_indexed_properties : WireEncryptedStringContext
    {
        [Test]
        public void Should_throw_exception()
        {
            var message = new MessageWithIndexedProperties();

            message[0] = Create();
            message[1] = Create();

            var exception = Assert.Throws<Exception>(() => inspector.ScanObject(message).ToList());
            Assert.AreEqual("Cannot encrypt or decrypt indexed properties that return a WireEncryptedString.", exception.Message);
        }

        public class MessageWithIndexedProperties : IMessage
        {
            WireEncryptedString[] indexedList = new WireEncryptedString[2];

            public WireEncryptedString this[int index]
            {
                get { return indexedList[index]; }
                set { indexedList[index] = value; }
            }
        }
    }

    [TestFixture]
    public class When_inspecting_a_message_with_circular_references : WireEncryptedStringContext
    {
        [Test]
        public void Should_match_the_property_correctly()
        {
            var child = new SubProperty {Secret = MySecretMessage};

            var message = new MessageWithCircularReferences
                {
                    Child = child
                };
            child.Self = child;
            child.Parent = message;

            var result = inspector.ScanObject(message).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Secret", result[0].Item2.Name);
        }
    }

    [TestFixture]
    public class When_inspecting_a_message_with_property_with_backing_public_field : WireEncryptedStringContext
    {
        [Test]
        public void Should_match_the_property_correctly()
        {
            var message = new MessageWithPropertyWithBackingPublicField
                {
                    MySecret = Create(),

                };

            inspector
                .ScanObject(message)
                .ToList()
                .ForEach(x => x.Item2.SetValue(x.Item1, (WireEncryptedString)MySecretMessage));

            Assert.AreEqual(MySecretMessage, message.MySecret.Value);
        }

        public class MessageWithPropertyWithBackingPublicField : IMessage
        {
            public WireEncryptedString mySuperSecret;

            public WireEncryptedString MySecret
            {
                get { return mySuperSecret; }
                set { mySuperSecret = value; }
            }
        }
    }

    [TestFixture]
    public class When_inspecting_a_message_with_a_encrypted_property_that_has_a_nonpublic_setter :
        WireEncryptedStringContext
    {
        [Test]
        public void Should_decrypt_correctly()
        {
            var message = new SecureMessageWithProtectedSetter(Create());

            inspector
                .ScanObject(message)
                .ToList()
                .ForEach(x => x.Item2.SetValue(x.Item1, Create()));

            Assert.AreEqual(message.Secret.Value, MySecretMessage);
        }
    }

    [TestFixture]
    public class When_decrypting_a_message_using_the_default_convention : WireEncryptedStringContext
    {
        [Test]
        public void Should_use_the_wireEncrypted_string()
        {
            var message = new Customer
                {
                    Secret = Create(),
                    SecretField = Create(),
                    CreditCard = new CreditCardDetails {CreditCardNumber = Create()}

            };

            inspector
                .ScanObject(message)
                .ToList()
                .ForEach(x => x.Item2.SetValue(x.Item1, Create()));

            Assert.AreEqual(MySecretMessage, message.Secret.Value);
            Assert.AreEqual(MySecretMessage, message.SecretField.Value);
            Assert.AreEqual(MySecretMessage, message.CreditCard.CreditCardNumber.Value);
        }
    }

    public class WireEncryptedStringContext
    {
        internal EncryptionInspector inspector;

        protected string EncryptedBase64Value = "encrypted value";
        protected string MySecretMessage = "A secret";
        protected Conventions conventions;

        [SetUp]
        public void BaseSetUp()
        {
            conventions = BuildConventions();
            inspector = new EncryptionInspector(conventions);
        }

        protected virtual Conventions BuildConventions()
        {
            return new Conventions
            {
                IsEncryptedPropertyAction = property => typeof(WireEncryptedString).IsAssignableFrom(property.PropertyType)
            };
        }

        protected WireEncryptedString Create()
        {
            return new WireEncryptedString
            {
                EncryptedValue = new EncryptedValue
                {
                    EncryptedBase64Value = EncryptedBase64Value,
                    Base64Iv = "init_vector"
                },
                Value = MySecretMessage
            };
        }
    }

    public class NonSecureMessage : IMessage
    {
        public string NotASecret { get; set; }
    }

    public class Customer : IMessage
    {
        public WireEncryptedString Secret { get; set; }
        public WireEncryptedString SecretField;
        public CreditCardDetails CreditCard { get; set; }

        public WireEncryptedString SecretThatIsNull { get; set; }

        public DateTime DateTime { get; set; }

        public List<CreditCardDetails> ListOfCreditCards { get; set; }

        public ArrayList ListOfSecrets { get; set; }

        public byte[] LargeByteArray{ get; set; }
    }

    public class CreditCardDetails
    {
        public WireEncryptedString CreditCardNumber { get; set; }
    }

    public class SecureMessageWithProtectedSetter : IMessage
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

    public class MessageWithMissingData : IMessage
    {
        public WireEncryptedString Secret { get; set; }
    }

    public class SubProperty
    {
        public MessageWithCircularReferences Parent { get; set; }
        public WireEncryptedString Secret { get; set; }
        public SubProperty Self { get; set; }
    }


    public class MessageWithLargePayload : IMessage
    {
        public byte[] LargeByteArray { get; set; }
    }
}
