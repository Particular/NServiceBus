namespace NServiceBus.Core.Tests.Serializers.XML
{
    using System;
    using NServiceBus.Serializers.XML.Test;
    using NUnit.Framework;

    public class GenericMessage<T1, T2>
    {
        public Guid SagaId { get; set; }

        public T1 Data1 { get; set; }

        public T2 Data2 { get; set; }

        public GenericMessage(Guid sagaId, T1 data1, T2 data2)
        {
            SagaId = sagaId;
            Data1 = data1;
            Data2 = data2;
        }
    }

    [TestFixture]
    public class SerializingGenericTests
    {
        [Test]
        public void CanSerializeAndBack()
        {
            var message = new GenericMessage<int, string>(Guid.NewGuid(), 1234, "Lorem ipsum");

            var result = ExecuteSerializer.ForMessage<GenericMessage<int, string>>(message);

            Assert.AreEqual(1234, result.Data1);
            Assert.AreEqual("Lorem ipsum", result.Data2);
        }
    }
}
