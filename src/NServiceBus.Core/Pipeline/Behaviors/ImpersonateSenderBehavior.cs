namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Threading;
    using Impersonation;

    class ImpersonateSenderBehavior : IBehavior
    {
        public ExtractIncomingPrincipal ExtractIncomingPrincipal { get; set; }

        public void Invoke(BehaviorContext context, Action next)
        {
            var principal = ExtractIncomingPrincipal.GetPrincipal(context.TransportMessage);

            if (principal == null)
            {
                next();
                return;
            }

            var previousPrincipal = Thread.CurrentPrincipal;
            try
            {
                context.Trace("Impersonating {0}", principal);
                Thread.CurrentPrincipal = principal;
                next();
            }
            finally
            {
                context.Trace("Reverting back to {0}", previousPrincipal);
                Thread.CurrentPrincipal = previousPrincipal;
            }
        }
    }
}