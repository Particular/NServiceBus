namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.Threading;
    using Impersonation;
    using Pipeline;
    using Pipeline.Contexts;

    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ImpersonateSenderBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
    public     ExtractIncomingPrincipal ExtractIncomingPrincipal { get; set;}


        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            if (!ConfigureImpersonation.Impersonate)
            {
                next();
                return;
            }

            var principal = ExtractIncomingPrincipal.GetPrincipal(context.PhysicalMessage);

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