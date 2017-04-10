namespace MessageMapperTests
{
    using System;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class When_requesting_unmapped_types
    {
        [Test]
        public void Should_initialize_requested_type()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new Type[]
            {
            });

            Assert.NotNull(mapper.GetMappedTypeFor(typeof(UnmappedType)));
        }

        public interface UnmappedType
        {
        }
    }
}