using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NUnit.Framework;

namespace NServiceBus.Serializers.Json.Test
{
  [TestFixture]
  public class JsonMessageSerializerTest : JsonMessageSerializerTestBase
  {
    protected override JsonMessageSerializerBase Serializer { get; set; }

    [SetUp]
    public void Setup()
    {
      var messageMapper = new MessageMapper();
      messageMapper.Initialize(new[] { typeof(IA), typeof(A) });

      Serializer = new JsonMessageSerializer(messageMapper);
    }
  }
}
