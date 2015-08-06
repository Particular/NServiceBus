namespace NServiceBus.Serializers
{
    using NServiceBus.Features;

    class AdditionalDeserializersSupport : Feature
    {
        public AdditionalDeserializersSupport()
        {
            EnableByDefault();
            Prerequisite(context => context.Settings.HasSetting("SelectedSerializer"), "Additional deserializers can be configured only when a default serializer exists");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            
        }
    }
}