namespace NServiceBus.Pipeline
{
    using System;
    using System.ComponentModel;
    using Contexts;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class PipelineOverride : IPipelineOverride
    {
        public virtual void Override(BehaviorList<IncomingContext> behaviorList)
        {
        }
        public virtual void Override(BehaviorList<OutgoingContext> behaviorList)
        {
        }
    }
}