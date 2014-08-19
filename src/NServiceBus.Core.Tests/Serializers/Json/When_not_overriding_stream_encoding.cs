namespace NServiceBus.Serializers.Json.Tests
{
    using System;
    using System.Text;
    using Features;
    using NUnit.Framework;

    [TestFixture]
    public class When_not_overriding_stream_encoding
    {
        Configure configure;

        [SetUp]
        public void SetUp()
        {
            var builder = new ConfigurationBuilder();

            builder.TypesToScan(new Type[0]);
            builder.UseSerialization<NServiceBus.Json>();

            configure = Configure.With(builder);

            var context = new FeatureConfigurationContext(configure);
            new JsonSerialization().SetupFeature(context);
        }

        [Test]
        public void Should_construct_serializer_that_uses_default_encoding()
        {
            var serializer = configure.Builder.Build<JsonMessageSerializer>();
            Assert.AreSame(Encoding.UTF8, serializer.Encoding);
        }
    }
}