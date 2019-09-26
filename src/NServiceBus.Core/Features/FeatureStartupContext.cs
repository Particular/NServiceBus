using NServiceBus.ObjectBuilder;

namespace NServiceBus
{
    class FeatureStartupContext
    {
        public FeatureStartupContext(IBuilder builder, IMessageSession messageSession)
        {
            Builder = builder;
            MessageSession = messageSession;
        }

        public IBuilder Builder { get; private set; }

        public IMessageSession MessageSession { get; private set; }
    }
}