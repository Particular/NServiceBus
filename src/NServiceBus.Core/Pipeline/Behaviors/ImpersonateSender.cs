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

            var currentPrincipal = Thread.CurrentPrincipal;
            try
            {
                Thread.CurrentPrincipal = principal;
                Next.Invoke(context);
            }
            finally
            {
                Thread.CurrentPrincipal = currentPrincipal;
            }
        }
    }
}