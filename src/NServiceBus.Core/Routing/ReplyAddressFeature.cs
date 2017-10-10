namespace NServiceBus.Features
{
    class ReplyAddressFeature : Feature
    {
        public ReplyAddressFeature()
        {
            EnableByDefault();
            Prerequisite(c => !c.Settings.Get<EndpointComponent>().IsSendOnly, "Send-Only endpoints can't receive replies");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var publicReturnAddress = context.Settings.GetOrDefault<string>("PublicReturnAddress");

            context.Pipeline.Register(
                new ApplyReplyToAddressBehavior(
                    context.Settings.LocalAddress(),
                    context.Settings.InstanceSpecificQueue(),
                    publicReturnAddress),
                "Applies the public reply to address to outgoing messages");
        }
    }
}