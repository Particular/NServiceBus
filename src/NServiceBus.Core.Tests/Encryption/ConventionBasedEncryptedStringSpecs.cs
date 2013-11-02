namespace NServiceBus.Core.Tests.Encryption
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class When_sending_a_message_with_user_defined_convention:UserDefinedConventionContext
    {
        [Test]
        public void Should_encrypt_the_value()
        {    
            var message = new ConventionBasedSecureMessage
                          {
                                  EncryptedSecret = "A secret"
                              };
            mutator.MutateOutgoing(message);

            Assert.AreEqual(string.Format("{0}@{1}", "encrypted value", "init_vector"), message.EncryptedSecret);
        }
    }

    [TestFixture]
    public class When_receiving_a_message_with_user_defined_convention : UserDefinedConventionContext
    {
        [Test]
        public void Should_encrypt_the_value()
        {
            var message = new ConventionBasedSecureMessage
                          {
                              EncryptedSecret = "encrypted value@init_vector"
                          };
            mutator.MutateIncoming(message);

            Assert.AreEqual("A secret", message.EncryptedSecret);
        }
    }

    [TestFixture]
    public class When_encrypting_a_property_that_is_not_a_string:UserDefinedConventionContext
    {
        [Test]
        public void Should_throw_an_exception()
        {
            Assert.Throws<InvalidOperationException>(() => mutator.MutateOutgoing(new MessageWithNonStringSecureProperty()));
        }
    }

    [TestFixture]
    public class When_decrypting_a_property_that_is_not_a_string : UserDefinedConventionContext
    {
        [Test]
        public void Should_throw_an_exception()
        {
            Assert.Throws<InvalidOperationException>(() => mutator.MutateIncoming(new MessageWithNonStringSecureProperty()));
        }
    }

    public class UserDefinedConventionContext : WireEncryptedStringContext
    {
        [SetUp]
        public void SetUp()
        {
            MessageConventionExtensions.IsEncryptedPropertyAction = p => p.Name.StartsWith("Encrypted");
        }
    }

    public class MessageWithNonStringSecureProperty
    {
        public int EncryptedInt { get; set; }
    }

    public class ConventionBasedSecureMessage:IMessage
    {
        public string EncryptedSecret { get; set; }
        public string EncryptedSecretThatIsNull { get; set; }
    }
}
