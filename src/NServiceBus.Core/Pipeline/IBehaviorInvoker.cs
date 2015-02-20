namespace NServiceBus.Pipeline
{
    using System;

    interface IBehaviorInvoker
    {
        void Invoke(object behavior, BehaviorContext context, Action<BehaviorContext> next);
    }
}