namespace NServiceBus.MSMQ.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class MsmqAddressTests
    {

        [Test]
        public void If_both_addresses_are_specified_via_host_name_it_should_not_convert()
        {
            var address = new MsmqAddress("replyToAddress", "replyToMachine");
            var returnAddress = address.MakeCompatibleWith(new MsmqAddress("someQueue","destinationmachine"), _ =>
            {
                throw new Exception("Should not call the lookup method");
            });
            Assert.AreEqual("replyToMachine", returnAddress.Machine);
        }

        [Test]
        public void If_both_addresses_are_specified_via_ip_it_should_not_convert()
        {
            var address = new MsmqAddress("replyToAddress", "202.171.13.141");
            var returnAddress = address.MakeCompatibleWith(new MsmqAddress("someQueue", "202.171.13.140"), _ =>
            {
                throw new Exception("Should not call the lookup method");
            });
            Assert.AreEqual("202.171.13.141", returnAddress.Machine);
        }
        
        [Test]
        public void If_reference_address_is_specified_via_ip_and_this_is_specified_via_host_name_it_should_convert_to_ip()
        {
            var address = new MsmqAddress("replyToAddress", "replyToMachine");
            var returnAddress = address.MakeCompatibleWith(new MsmqAddress("someQueue", "202.171.13.140"), _ => "10.10.10.10");
            Assert.AreEqual("10.10.10.10", returnAddress.Machine);
        }
    }
}