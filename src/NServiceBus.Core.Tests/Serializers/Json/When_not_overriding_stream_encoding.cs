namespace NServiceBus.Serializers.Json.Tests
{
    using System.Text;
    using MessageInterfaces.MessageMapper.Reflection;
    using Settings;
    using NUnit.Framework;

    [TestFixture]
    public class When_not_overriding_stream_encoding
    {

        [Test]
        public void Should_construct_serializer_that_uses_default_encoding()
        {
            var settings = new SettingsHolder();

            var serializer = (JsonMessageSerializer)new JsonSerializer().Configure(settings)(new MessageMapper());

            Assert.AreSame(Encoding.UTF8, serializer.Encoding);
        }
    }
}