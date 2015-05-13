namespace NServiceBus.Pipeline
{
    using System;
    using System.Threading.Tasks;

    interface IBehaviorInvoker
    {
        Task Invoke(object behavior, BehaviorContext context, Func<BehaviorContext, Task> next);
    }
}