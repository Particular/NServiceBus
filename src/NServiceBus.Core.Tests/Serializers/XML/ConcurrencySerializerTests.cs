namespace NServiceBus.Serializers.XML.Test
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using MessageInterfaces.MessageMapper.Reflection;
    using Serialization;

    [TestFixture]
    public class ConcurrencySerializerTests
    {
        [Test]
        public void Should_deserialize_in_parallel()
        {
            var expected = new RequestDataMessage
                               {
                                   DataId = Guid.Empty,
                                   String = "<node>it's my \"node\" & i like it<node>",
                               };

            var serializer = SerializerFactory.Create<RequestDataMessage>();

            Parallel.For(1, 1000, i =>
                                      {
                                          RequestDataMessage result;
                                          using (var stream = new MemoryStream())
                                          {
                                              serializer.Serialize(expected, stream);
                                              stream.Position = 0;

                                              var msgArray = serializer.Deserialize(stream);
                                              result = (RequestDataMessage) msgArray[0];
                                          }

                                          Assert.AreEqual(expected.DataId, result.DataId);
                                          Assert.AreEqual(expected.String, result.String);
                                    });
        }

        [Test]
        public void Should_serializer_without_NRE()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[] { typeof(MyCommand) });

            var conventions = new Conventions();
            conventions.IsCommandTypeAction = t => t == typeof(MyCommand);

            IMessageSerializer s = new XmlMessageSerializer(mapper, conventions);

            var m = new MyCommand();

            var po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 10;

            Parallel.For(0, 10, po, i =>
             {
                 using (var ms = new MemoryStream())
                 {
                     s.Serialize(m, ms);
                 }
             });
        }

        class MyCommand {}
    }

    public class RequestDataMessage : IMessage
    {
        public Guid DataId { get; set; }
        public string String { get; set; }
    }
}
