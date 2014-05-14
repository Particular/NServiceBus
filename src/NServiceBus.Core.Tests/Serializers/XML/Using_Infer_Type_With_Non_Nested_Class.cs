namespace NServiceBus.Core.Tests.Serializers.XML
{
    using System.IO;
    using NServiceBus.Serializers.XML.Test;
    using NUnit.Framework;

    [TestFixture]
    public class Using_Infer_Type_With_Non_Nested_Class
    {
        [Test]
        public void Execute()
        {
            var xml = @"<?xml version=""1.0"" ?>
<Messages 
    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
    xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
    xmlns=""http://tempuri.net/NServiceBus.Core.Tests.Serializers.XML""
    xmlns:baseType=""NServiceBus.Core.Tests.Serializers.XML.IMyBusMessage"">
    <M1></M1>
    <M2></M2>
    <M2></M2>
    <M2></M2>
</Messages>
";

            using (Stream stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream);
                writer.Write(xml);
                writer.Flush();
                stream.Position = 0;

                var serializer = SerializerFactory.Create(typeof(IMyBusMessage), typeof(M1), typeof(M2));
                var messageDeserialized = serializer.Deserialize(stream);
                Assert.IsInstanceOf<M1>(messageDeserialized[0]);
                Assert.IsInstanceOf<M2>(messageDeserialized[1]);
                Assert.IsInstanceOf<M2>(messageDeserialized[2]);
                Assert.IsInstanceOf<M2>(messageDeserialized[3]);
            }
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