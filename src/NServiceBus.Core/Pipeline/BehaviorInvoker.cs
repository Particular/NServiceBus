namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;

    class BehaviorInvoker<TIn, TOut> : IBehaviorInvoker 
        where TOut : IBehaviorContext
        where TIn : IBehaviorContext
    {
        public Task Invoke(object behavior, IBehaviorContext context, Func<IBehaviorContext, Task> next)
        {
            return ((IBehavior<TIn, TOut>)behavior).Invoke((TIn)context, next as Func<TOut, Task>);
        }
    }
}