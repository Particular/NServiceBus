namespace NServiceBus.Encryption.Tests
{
    using Core.Tests.Encryption;
    using NUnit.Framework;

    [TestFixture]
    public class Issue_949 : WireEncryptedStringContext
    {
        [Test]
        public void null_element_in_primitive_array()
        {
            var message = new TestMessageWithPrimitives();
            message.Data = new int?[] { null, 1 };

            mutator.MutateOutgoing(message);

            Assert.AreEqual(new int?[] { null, 1}, message.Data);
        }

        [Test]
        public void null_element_in_object_array()
        {
            var message = new TestMessageWithObjects();
            message.Data = new object[] { null, this, null };
            
            mutator.MutateOutgoing(message);

            Assert.AreEqual(new object[] { null, this,null }, message.Data);
            
        }

        private class TestMessageWithPrimitives
        {
            public int?[] Data;
        }

        private class TestMessageWithObjects
        {
            public object[] Data;
        }
    }
}
