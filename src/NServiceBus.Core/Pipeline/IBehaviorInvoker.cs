namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;

    interface IBehaviorInvoker
    {
        Task Invoke(object behavior, BehaviorContext context, Func<BehaviorContext, Task> next);
    }
}