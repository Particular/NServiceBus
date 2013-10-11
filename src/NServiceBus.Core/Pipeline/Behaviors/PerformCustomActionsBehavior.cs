namespace NServiceBus.Pipeline.Behaviors
{
    using System;

    public class PerformCustomActionsBehavior : IBehavior
    {
        public IBehavior Next { get; set; }

        public string Label { get; set; }

        public Action<IBehaviorContext> Before { get; set; }

        public Action<IBehaviorContext> After { get; set; }

        public void Invoke(IBehaviorContext context)
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