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
            var xml = @"<?xml version=""1.0"" ?>
<Messages 
    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
    xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
    xmlns=""http://tempuri.net/NameSpace2"" 
    xmlns:q1=""http://tempuri.net/NameSpace1"" 
    xmlns:baseType=""NameSpace1.IMyBusMessage"">
    <M1></M1>
    <q1:M1></q1:M1>
</Messages>
";
            using (Stream stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream);
                writer.Write(xml);
                writer.Flush();
                stream.Position = 0;

                var serializer = SerializerFactory.Create(typeof(IMyBusMessage), typeof(NameSpace1.M1), typeof(NameSpace2.M1));

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