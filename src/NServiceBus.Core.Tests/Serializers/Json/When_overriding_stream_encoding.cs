namespace NServiceBus.Serializers.Json.Tests
{
    using System.Text;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
    using NServiceBus.Serialization;
    using NServiceBus.Settings;
    using NUnit.Framework;

    [TestFixture]
    public class When_overriding_stream_encoding
    {
        [Test]
        public void Should_construct_serializer_that_uses_requested_encoding()
        {
            var settings = new SettingsHolder();
            var extensions = new SerializationExtentions<JsonSerializer>(settings);
            extensions.Encoding(Encoding.UTF7);

            var serializer = (NServiceBus.JsonMessageSerializer)new JsonSerializer().Configure(settings)(new MessageMapper());
            Assert.AreSame(Encoding.UTF7, serializer.Encoding);
        }
    }
}