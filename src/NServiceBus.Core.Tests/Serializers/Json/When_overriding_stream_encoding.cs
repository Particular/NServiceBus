namespace NServiceBus.Serializers.Json.Tests
{
    using System.Text;
    using System.Threading.Tasks;
    using Features;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class When_overriding_stream_encoding
    {
        [Test]
        public async Task Should_construct_serializer_that_uses_requested_encoding()
        {
            var builder = new BusConfiguration();

            builder.SendOnly();
            builder.TypesToScanInternal(new[] {typeof(EncodingValidatorFeature) });
            builder.UseSerialization<JsonSerializer>().Encoding(Encoding.UTF7);
            builder.EnableFeature<EncodingValidatorFeature>();

            var endpoint = await Endpoint.Start(builder);
            await endpoint.Stop();
        }

        class EncodingValidatorFeature : Feature
        {
            protected internal override void Setup(FeatureConfigurationContext context)
            {
                context.RegisterStartupTask(b => new ValidatorTask(b));
            }

            class ValidatorTask : FeatureStartupTask
            {
                IBuilder builder;

                public ValidatorTask(IBuilder builder)
                {
                    this.builder = builder;
                }

                protected override Task OnStart(IBusSession session)
                {
                    var serializer = builder.Build<JsonMessageSerializer>();
                    Assert.AreSame(Encoding.UTF7, serializer.Encoding);
                    return Task.FromResult(0);
                }

                protected override Task OnStop(IBusSession session)
                {
                    return TaskEx.Completed;
                }
            }
        }
    }
}