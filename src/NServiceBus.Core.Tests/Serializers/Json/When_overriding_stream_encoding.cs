namespace NServiceBus.Serializers.Json.Tests
{
    using System;
    using System.Text;
    using Features;
    using NUnit.Framework;

    [TestFixture]
    public class When_overriding_stream_encoding
    {
        Configure configure;

        [SetUp]
        public void SetUp()
        {
            var builder = new ConfigurationBuilder();
            
            builder.TypesToScan(new Type[0]);
            builder.UseSerialization<NServiceBus.Json>().Encoding(Encoding.UTF7);

            configure = Configure.With(builder);

            var context = new FeatureConfigurationContext(configure);
            new JsonSerialization().SetupFeature(context);
        }

        [Test]
        public void Should_construct_serializer_that_uses_requested_encoding()
        {
            var serializer = configure.Builder.Build<JsonMessageSerializer>();
            Assert.AreSame(Encoding.UTF7, serializer.Encoding);
        }
    }
}