namespace NServiceBus.Serializers.Json.Tests
{
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Features;
    using NServiceBus.ObjectBuilder;
    using NUnit.Framework;

    [TestFixture]
    public class When_not_overriding_stream_encoding
    {
     
        [Test]
        public async Task Should_construct_serializer_that_uses_default_encoding()
        {
            var builder = new BusConfiguration();

            builder.SendOnly();
            builder.TypesToScanInternal(new[] { typeof(EncodingValidatorFeature) });
            builder.UseSerialization<JsonSerializer>();
            builder.EnableFeature<EncodingValidatorFeature>();

            var endpoint = await Endpoint.Start(builder);
            await endpoint.Stop();
        }

        class EncodingValidatorFeature : Feature
        {
            public EncodingValidatorFeature()
            {
                RegisterStartupTask<ValidatorTask>();
            }

            protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
            {
                return FeatureStartupTask.None;
            }

            class ValidatorTask : FeatureStartupTask
            {
                IBuilder builder;

                public ValidatorTask(IBuilder builder)
                {
                    this.builder = builder;
                }

                protected override Task OnStart(IBusContext context)
                {
                    var serializer = builder.Build<JsonMessageSerializer>();
                    Assert.AreSame(Encoding.UTF8, serializer.Encoding);
                    return Task.FromResult(0);
                }
            }
        }
    }
}