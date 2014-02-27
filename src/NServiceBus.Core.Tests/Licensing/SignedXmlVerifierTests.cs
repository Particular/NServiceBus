namespace NServiceBus.Core.Tests.Licensing
{
    using System;
    using NServiceBus.Licensing;
    using NUnit.Framework;

    [TestFixture]
    public class SignedXmlVerifierTests
    {
        [Test]
        public void Non_xml_should_throw()
        {
            var exception = Assert.Throws<Exception>(() => SignedXmlVerifier.VerifyXml(SignedXmlVerifier.PublicKey, "sdfsdf"));
            Assert.AreEqual("The text provided could not be parsed as XML.", exception.Message);
        }

        [Test]
        public void Valid_xml_does_not_throw()
        {
            var validXml = ResourceReader.ReadResourceAsString("Licensing.SignedValid.xml");
            SignedXmlVerifier.VerifyXml(SignedXmlVerifier.PublicKey, validXml);
        }

        [Test]
        public void Invalid_xml_is_not_verified()
        {
            var invalidXml = ResourceReader.ReadResourceAsString("Licensing.SignedInvalid.xml");
            var exception = Assert.Throws<Exception>(() => SignedXmlVerifier.VerifyXml(SignedXmlVerifier.PublicKey, invalidXml));
            Assert.AreEqual("License is invalid as it failed signature check.", exception.Message);
        }
    }
}