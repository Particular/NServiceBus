namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    interface IBehaviorInvoker
    {
        Task Invoke(object behavior, IBehaviorContext context, Func<IBehaviorContext, Task> next);
    }
}