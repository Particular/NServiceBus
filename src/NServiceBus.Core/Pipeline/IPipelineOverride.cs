namespace NServiceBus.Pipeline
{
    using System;
    using System.ComponentModel;
    using Contexts;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IPipelineOverride
    {
        void Override(BehaviorList<IncomingContext> behaviorList);
        void Override(BehaviorList<OutgoingContext> behaviorList);
    }
}