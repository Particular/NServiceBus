namespace NServiceBus.Core.Tests.Serializers.XML
{
    using System.IO;
    using NServiceBus.Serializers.XML.Test;
    using NUnit.Framework;

    [TestFixture]
    public class Using_Infer_Type_With_Mixed_Namespace
    {
        [Test]
        public void Execute()
        {
            var m1 = new NameSpace1.M1();
            var m2_1 = new NameSpace2.M1();

            using (Stream stream = new MemoryStream())
            {
                var serializer = SerializerFactory.Create(typeof(IMyBusMessage), typeof(NameSpace1.M1), typeof(NameSpace2.M1));
                serializer.Serialize(new object[] { m2_1, m1 }, stream);

                stream.Position = 0;
                //   var readToEnd = new StreamReader(stream).ReadToEnd();

                var messageDeserialized = serializer.Deserialize(stream);
                Assert.IsInstanceOf<NameSpace2.M1>(messageDeserialized[0]);
                Assert.IsInstanceOf<NameSpace1.M1>(messageDeserialized[1]);
            }
        }
    }

}

namespace NameSpace1
{
    using NServiceBus;

    public interface IMyBusMessage : IMessage
    {
    }

    public class M1 : IMyBusMessage
    {
    }

}

namespace NameSpace2
{
    using NameSpace1;

    public class M1 : IMyBusMessage
    {
    }
}