namespace NServiceBus.Pipeline.Behaviors
{
    using System;

    public class PerformCustomActions : IBehavior
    {
        public IBehavior Next { get; set; }

        public Action Before { get; set; }
        
        public Action After { get; set; }

        public void Invoke(IBehaviorContext context)
        {
            if (Before != null)
            {
                context.Trace("Executing custom -Before- action");
                Before();
            }

            Next.Invoke(context);

            if (After != null)
            {
                context.Trace("Executing custom -After- action");
                After();
            }
        }
    }
}