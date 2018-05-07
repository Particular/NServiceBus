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
    xmlns=""http://tempuri.net/Namespace2""
    xmlns:q1=""http://tempuri.net/Namespace1""
    xmlns:baseType=""Namespace1.IMyBusMessage"">
    <FirstMessage></FirstMessage>
    <q1:FirstMessage></q1:FirstMessage>
</Messages>
";
            using (Stream stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream);
                writer.Write(xml);
                writer.Flush();
                stream.Position = 0;

                var serializer = SerializerFactory.Create(typeof(IMyBusMessage), typeof(Namespace1.FirstMessage), typeof(Namespace2.FirstMessage));

                var messageDeserialized = serializer.Deserialize(stream);
                Assert.IsInstanceOf<Namespace2.FirstMessage>(messageDeserialized[0]);
                Assert.IsInstanceOf<Namespace1.FirstMessage>(messageDeserialized[1]);
            }
        }
    }

}

namespace Namespace1
{
    using NServiceBus;

    public interface IMyBusMessage : IMessage
    {
    }

    public class FirstMessage : IMyBusMessage
    {
    }

}

namespace Namespace2
{
    using Namespace1;

    public class FirstMessage : IMyBusMessage
    {
    }
}