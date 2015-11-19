namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MessageMutator;
    using OutgoingPipeline;
    using Pipeline;
    using Pipeline.Contexts;

    class MutateOutgoingTransportMessageBehavior : Behavior<OutgoingPhysicalMessageContext>
    {
        public override async Task Invoke(OutgoingPhysicalMessageContext context, Func<Task> next)
        {
            var outgoingMessage = context.Extensions.Get<OutgoingLogicalMessage>();

            InvokeHandlerContextImpl incomingState;
            context.Extensions.TryGetRootContext(out incomingState);

            object messageBeingHandled = null;
            Dictionary<string, string> incomingHeaders = null;
            if (incomingState != null)
            {
                messageBeingHandled = incomingState.MessageBeingHandled;
                incomingHeaders = incomingState.Headers;
            }

            var mutatorContext = new MutateOutgoingTransportMessageContext(
                context.Body, 
                outgoingMessage.Instance, 
                context.Headers, 
                messageBeingHandled, 
                incomingHeaders);
            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingTransportMessages>())
            {
                await mutator.MutateOutgoing(mutatorContext).ConfigureAwait(false);
            }
            context.Body = mutatorContext.OutgoingBody;

            await next().ConfigureAwait(false);
        }
    }
}