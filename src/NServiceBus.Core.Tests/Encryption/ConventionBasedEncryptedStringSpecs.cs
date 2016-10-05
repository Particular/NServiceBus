namespace NServiceBus.Core.Tests.Encryption
{
    using System;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class When_inspecting_a_message_with_user_defined_convention : UserDefinedConventionContext
    {
        [Test]
        public void Should_return_the_value()
        {
            var message = new ConventionBasedSecureMessage
            {
                EncryptedSecret = "A secret"
            };

            var result = inspector.ScanObject(message).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("EncryptedSecret", result[0].Item2.Name);
        }
    }

    [TestFixture]
    public class When_inspecting_a_property_that_is_not_a_string : UserDefinedConventionContext
    {
        [Test]
        public void Should_throw_an_exception()
        {
            var exception = Assert.Throws<Exception>(() => inspector.ScanObject(new MessageWithNonStringSecureProperty()).ToList());
            Assert.AreEqual("Only string properties are supported for convention based encryption. Check the configured conventions.", exception.Message);
        }
    }

    public class UserDefinedConventionContext : WireEncryptedStringContext
    {
        protected override Conventions BuildConventions()
        {
            return new Conventions
            {
                IsEncryptedPropertyAction = p => p.Name.StartsWith("Encrypted")
            };
        }
    }

    public class MessageWithNonStringSecureProperty
    {
        public int EncryptedInt { get; set; }
    }

    public class ConventionBasedSecureMessage : IMessage
    {
        public string EncryptedSecret { get; set; }
        public string EncryptedSecretThatIsNull { get; set; }
    }
}
