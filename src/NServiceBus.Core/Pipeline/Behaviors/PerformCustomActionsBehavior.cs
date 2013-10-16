namespace NServiceBus.Pipeline.Behaviors
{
    using System;

    class PerformCustomActionsBehavior : IBehavior
    {
        public IBehavior Next { get; set; }

        public string Label { get; set; }

        public Action<BehaviorContext> Before { get; set; }

        public Action<BehaviorContext> After { get; set; }

        public void Invoke(BehaviorContext context)
        {
            if (Before != null)
            {
                context.Trace("Executing custom -Before- action: {0}", Label);
                Before(context);
            }

            Next.Invoke(context);

            if (After != null)
            {
                context.Trace("Executing custom -After- action: {0}", Label);
                After(context);
            }
        }
    }
}