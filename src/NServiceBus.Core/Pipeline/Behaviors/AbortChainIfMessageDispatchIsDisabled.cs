namespace NServiceBus.Pipeline.Behaviors
{
    using System;

    /// <summary>
    /// Aborts the chain if message dispatch has been disabled. Message dispatch can still be disabled later
    /// in the chain, but this way we have the ability to abort before doing more work than necessary
    /// </summary>
    class AbortChainIfMessageDispatchIsDisabled : IBehavior
    {
        public void Invoke(BehaviorContext context, Action next)
        {
            // this one might have been set by any behavior earlier on in the chain like mutators and whatnot
            if (context.DoNotContinueDispatchingMessageToHandlers)
            {
                return;
            }

            next();
        }

    }
}