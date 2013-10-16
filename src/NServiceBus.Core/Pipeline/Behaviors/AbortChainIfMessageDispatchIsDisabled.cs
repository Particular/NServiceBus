namespace NServiceBus.Pipeline.Behaviors
{
    /// <summary>
    /// Aborts the chain if message dispatch has been disabled. Message dispatch can still be disabled later
    /// in the chain, but this way we have the ability to abort before doing more work than necessary
    /// </summary>
    class AbortChainIfMessageDispatchIsDisabled : IBehavior
    {
        public IBehavior Next { get; set; }

        public void Invoke(BehaviorContext context)
        {
            // this one might have been set by any behavior earlier on in the chain like mutators and whatnot
            if (context.DoNotContinueDispatchingMessageToHandlers)
            {
                return;
            }

            Next.Invoke(context);
        }
    }
}