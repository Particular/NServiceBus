namespace NServiceBus.Core.Tests
{
    using Gateway.Utils;
    using NUnit.Framework;

    [TestFixture]
    public class HasherTests
    {

        [Test]
        public void Valid_Md5_can_be_verified()
        {
            Hasher.Verify("myData".ConvertToStream(), Hasher.Hash("myData".ConvertToStream()));
        }

        [Test]
        public void Invalid_hash_throws_ChannelException()
        {
            Hasher.Verify("myData".ConvertToStream(), "invalidHash");
        }

    }
}