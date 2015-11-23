﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MessageMutator;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using Pipeline;
    using Pipeline.Contexts;

    class MutateOutgoingMessagesBehavior : Behavior<OutgoingLogicalMessageContext>
    {
        public override async Task Invoke(OutgoingLogicalMessageContext context, Func<Task> next)
        {
            //TODO: should not need to do a lookup
            var state = context.Get<OutgoingPhysicalToRoutingConnector.State>();
            InvokeHandlerContext incomingState;
            context.TryGetRootContext(out incomingState);

            object messageBeingHandled = null;
            Dictionary<string, string> incomingHeaders = null;
            if (incomingState != null)
            {
                messageBeingHandled = incomingState.MessageBeingHandled;
                incomingHeaders = incomingState.Headers;
            }
            var mutatorContext = new MutateOutgoingMessageContext(
                context.Message.Instance, 
                state.Headers,
                messageBeingHandled, 
                incomingHeaders);

            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingMessages>())
            {
                await mutator.MutateOutgoing(mutatorContext).ConfigureAwait(false);
            }

            if (mutatorContext.MessageInstanceChanged)
            {
                context.UpdateMessageInstance(mutatorContext.OutgoingMessage);
            }

            await next().ConfigureAwait(false);
        }
    }
}