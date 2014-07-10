namespace NServiceBus.Core.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class AddressTests
    {
        [Test, Explicit("We can't run this test in CI because Address uses statics and setting this flag will make other tests fail. And the flag can't be unset!")]
        public void HashCode_ignores_machine_name()
        {
            Address.IgnoreMachineName();

            var address1 = new Address("MyEndpoint", "Server1");
            var address2 = new Address("MyEndpoint", "Server2");

            Assert.AreEqual(address1.GetHashCode(), address2.GetHashCode());
        }
    }
}
