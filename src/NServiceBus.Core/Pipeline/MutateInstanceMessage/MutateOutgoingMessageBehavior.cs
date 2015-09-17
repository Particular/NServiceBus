namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.MessageMutator;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class MutateOutgoingMessageBehavior : Behavior<OutgoingContext>
    {
        public override async Task Invoke(OutgoingContext context, Func<Task> next)
        {
            //TODO: should not need to do a lookup
            var state = context.Get<DispatchMessageToTransportConnector.State>();
            HandlingStageBehavior.Context incomingState;
            context.TryGetRootContext(out incomingState);

            object messageBeingHandled = null;
            Dictionary<string, string> incomingHeaders = null;
            if (incomingState != null)
            {
                messageBeingHandled = incomingState.MessageBeingHandled;
                incomingHeaders = incomingState.Headers;
            }
            var mutatorContext = new MutateOutgoingMessageContext(
                context.GetMessageInstance(), 
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