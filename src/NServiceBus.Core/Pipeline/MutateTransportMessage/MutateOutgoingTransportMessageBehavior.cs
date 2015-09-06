namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.MessageMutator;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline.Contexts;

    class MutateOutgoingTransportMessageBehavior : PhysicalOutgoingContextStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            //TODO: should not need to do a lookup
            var state = context.Get<DispatchMessageToTransportConnector.State>();
            var outgoingMessage = context.Get<OutgoingLogicalMessage>();

            HandlingStageBehavior.Context incomingState;
            context.TryGetRootContext(out incomingState);

            object messageBeingHandled = null;
            Dictionary<string, string> incomingHeaders = null;
            if (incomingState != null)
            {
                messageBeingHandled = incomingState.MessageBeingHandled;
                incomingHeaders = incomingState.Headers;
            }

            var mutatorContext = new MutateOutgoingTransportMessageContext(context.Body, outgoingMessage.Instance, state
                .Headers, messageBeingHandled, incomingHeaders);
            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingTransportMessages>())
            {
                mutator.MutateOutgoing(mutatorContext);
            }
            context.Body = mutatorContext.OutgoingBody;

            next();
        }
    }
}