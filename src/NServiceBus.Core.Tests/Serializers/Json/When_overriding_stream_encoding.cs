namespace NServiceBus.Serializers.Json.Tests
{
    using System.Text;
    using Features;
    using NUnit.Framework;

    [TestFixture]
    public class When_overriding_stream_encoding
    {
        [Test]
        public void Should_construct_serializer_that_uses_requested_encoding()
        {
            var builder = new BusConfiguration();

            builder.UseSerialization<JsonSerializer>().Encoding(Encoding.UTF7);

            var config = builder.BuildConfiguration();

            var context = new FeatureConfigurationContext(config);
            new JsonSerialization().SetupFeature(context);

            var serializer = config.Builder.Build<JsonMessageSerializer>();
            Assert.AreSame(Encoding.UTF7, serializer.Encoding);
        }
    }
}