namespace NServiceBus.Pipeline
{
    using System;
    using System.Threading.Tasks;

    class BehaviorInvoker<TIn, TOut> : IBehaviorInvoker 
        where TOut : BehaviorContext
        where TIn : BehaviorContext
    {
        public Task Invoke(object behavior, BehaviorContext context, Func<BehaviorContext, Task> next)
        {
            return ((IBehavior<TIn, TOut>)behavior).Invoke((TIn)context, next);
        }
    }
}