namespace NServiceBus.Testing
{
    using NServiceBus.Features;
    using Transports;

    class FakeTestTransportConfigurer: ConfigureTransport<FakeTestTransport>
    {
        

        protected override void Configure(FeatureConfigurationContext context, string connectionString)
        {
            
        }

        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "FooBar"; }
        }
    }
}