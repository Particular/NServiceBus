namespace NServiceBus.Pipeline
{
    using System;

    class BehaviorInvoker<TIn, TOut> : IBehaviorInvoker 
        where TOut : BehaviorContext
        where TIn : BehaviorContext
    {
        public void Invoke(object behavior, BehaviorContext context, Action<BehaviorContext> next)
        {
            ((IBehavior<TIn, TOut>)behavior).Invoke((TIn)context, next);
        }
    }
}