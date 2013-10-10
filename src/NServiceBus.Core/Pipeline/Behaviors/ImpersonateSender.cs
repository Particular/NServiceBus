namespace NServiceBus.Pipeline.Behaviors
{
    using System.Threading;
    using Impersonation;

    public class ImpersonateSender : IBehavior
    {
        readonly ExtractIncomingPrincipal extractIncomingPrincipal;

        public ImpersonateSender(ExtractIncomingPrincipal extractIncomingPrincipal)
        {
            this.extractIncomingPrincipal = extractIncomingPrincipal;
        }

        public IBehavior Next { get; set; }

        public void Invoke(IBehaviorContext context)
        {
            var principal = extractIncomingPrincipal.GetPrincipal(context.TransportMessage);

            if (principal == null)
                return;

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