namespace NServiceBus.Pipeline.Behaviors
{
    using System;

    class PerformCustomActionsBehavior : IBehavior
    {
        public string Label { get; set; }

        public Action<BehaviorContext> Before { get; set; }

        public Action<BehaviorContext> After { get; set; }

        public void Invoke(BehaviorContext context, Action next)
        {
            if (Before != null)
            {
                context.Trace("Executing custom -Before- action: {0}", Label);
                Before(context);
            }

            next();

            if (After != null)
            {
                context.Trace("Executing custom -After- action: {0}", Label);
                After(context);
            }
        }
    }
}