namespace NServiceBus.Pipeline
{
    using System.Collections;
    using ObjectBuilder;

    /// <summary>
    /// Behavior implementation that can wrap another behavior, lizily resolving it from the builder when it is time
    /// </summary>
    public class LazyBehavior<TBehavior> : IBehavior
        where TBehavior : IBehavior
    {
        readonly IBuilder builder;

        public LazyBehavior(IBuilder builder)
        {
            this.builder = builder;
        }

        public IBehavior Next { get; set; }

        public void Invoke(IBehaviorContext context)
        {
            var behaviorInstance = builder.Build<TBehavior>();
            behaviorInstance.Next = Next;
            behaviorInstance.Invoke(context);
        }
    }
}