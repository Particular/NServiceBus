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
                Thread.CurrentPrincipal = principal;
                next();
            }
            finally
            {
                Thread.CurrentPrincipal = previousPrincipal;
            }
        }
    }
}