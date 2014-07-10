namespace NServiceBus.Core.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class AddressTests
    {
        [Test]
        public void HashCode_ignores_machine_name()
        {
            Address.IgnoreMachineName();

            var address1 = new Address("MyEndpoint", "Server1");
            var address2 = new Address("MyEndpoint", "Server2");

            Assert.AreEqual(address1.GetHashCode(), address2.GetHashCode());
        }
    }
}
