namespace NServiceBus.Features
{
    using Config;
    using NServiceBus.Outbox;

    public class Outbox:Feature
    {
        public override void Initialize()
        {
            InfrastructureServices.Enable<IOutboxStorage>();
        }
    }
}