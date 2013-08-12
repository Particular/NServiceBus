namespace NServiceBus.Core.Tests.Serializers.XML
{
    using System.IO;
    using NServiceBus.Serializers.XML.Test;
    using NUnit.Framework;

    [TestFixture]
    public class Using_Inferred_Nested_Types
    {
        [Test]
        [Explicit]
        public void Should_throw_an_exception()
        {
           var m2_1 = new M2();
           var m2_2 = new M2();
           var m2_3 = new M2();

           using (Stream stream = new MemoryStream())
           {
               var serializer = SerializerFactory.Create(typeof(IMyBusMessage), typeof(M1), typeof(M2));
               //var exception = Assert.Throws<Exception>(() => serializer.Initialize(new[] { typeof(IMyBusMessage), typeof(M1), typeof(M2) }));
               //Assert.AreEqual("Nested types are not supported by the XmlMessageSerializer.", exception.Message);
               var m1 = new M1();
               serializer.Serialize(new object[] {m1, m2_1, m2_2, m2_3}, stream);
               stream.Position = 0;
               var messageDeserialized = serializer.Deserialize(stream);
               Assert.IsInstanceOf<M1>(messageDeserialized[0]);
               Assert.IsInstanceOf<M2>(messageDeserialized[1]);
               Assert.IsInstanceOf<M2>(messageDeserialized[2]);
               Assert.IsInstanceOf<M2>(messageDeserialized[3]);
           }

        }

        public interface IMyBusMessage : IMessage
        {
        }

        public class M1 : IMyBusMessage
        {
        }

        public class M2 : IMyBusMessage
        {
        }
    }
}
