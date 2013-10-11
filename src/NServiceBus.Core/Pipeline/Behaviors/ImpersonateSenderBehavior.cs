namespace NServiceBus.Pipeline.Behaviors
{
    using System.Threading;
    using Impersonation;

    public class ImpersonateSenderBehavior : IBehavior
    {
        public ExtractIncomingPrincipal ExtractIncomingPrincipal { get; set; }

        public IBehavior Next { get; set; }

        public void Invoke(IBehaviorContext context)
        {
            var principal = ExtractIncomingPrincipal.GetPrincipal(context.TransportMessage);

            if (principal == null)
            {
                Next.Invoke(context);
                return;
            }

            var previousPrincipal = Thread.CurrentPrincipal;
            try
            {
                context.Trace("Impersonating {0}", principal);
                Thread.CurrentPrincipal = principal;
                Next.Invoke(context);
            }
            finally
            {
                context.Trace("Reverting back to {0}", previousPrincipal);
                Thread.CurrentPrincipal = previousPrincipal;
            }
        }
    }
}