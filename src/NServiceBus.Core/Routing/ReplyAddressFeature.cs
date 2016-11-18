namespace NServiceBus.Features
{
    class ReplyAddressFeature : Feature
    {
        public ReplyAddressFeature()
        {
            EnableByDefault();
            Prerequisite(c => !c.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Send-Only endpoints can't receive replies");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var publicReturnAddress = context.Settings.GetOrDefault<string>("PublicReturnAddress");
            var distributorAddress = context.Settings.GetOrDefault<string>("LegacyDistributor.Address");
            context.Pipeline.Register(
                new ApplyReplyToAddressBehavior(
                    context.Settings.LocalAddress(),
                    context.Settings.InstanceSpecificQueue(),
                    publicReturnAddress,
                    distributorAddress),
                "Applies the public reply to address to outgoing messages");
        }
    }
}