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
            if (Before != null) Before();

            Next.Invoke(context);

            if (After != null) After();
        }
    }
}