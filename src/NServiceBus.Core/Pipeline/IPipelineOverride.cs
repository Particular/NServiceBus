namespace NServiceBus.Pipeline
{
    using System;
    using System.ComponentModel;
    using Contexts;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IPipelineOverride
    {
        void Override(BehaviorList<HandlerInvocationContext> behaviorList);
        void Override(BehaviorList<ReceiveLogicalMessageContext> behaviorList);
        void Override(BehaviorList<ReceivePhysicalMessageContext> behaviorList);
        void Override(BehaviorList<SendLogicalMessageContext> behaviorList);
        void Override(BehaviorList<SendPhysicalMessageContext> behaviorList);
    }
}