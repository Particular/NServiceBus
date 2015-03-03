namespace NServiceBus.Serializers.Json.Tests
{
    using System.Text;
    using Features;
    using NUnit.Framework;

    [TestFixture]
    public class When_not_overriding_stream_encoding
    {
     
        [Test]
        public void Should_construct_serializer_that_uses_default_encoding()
        {
            var builder = new BusConfiguration();

            builder.UseSerialization<JsonSerializer>();

            var config = builder.BuildConfiguration();

            var context = new FeatureConfigurationContext(config);
            new JsonSerialization().SetupFeature(context);
   
            var serializer = config.Builder.Build<JsonMessageSerializer>();
            Assert.AreSame(Encoding.UTF8, serializer.Encoding);
        }
    }
}