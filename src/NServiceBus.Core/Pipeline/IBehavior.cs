namespace NServiceBus.Pipeline
{
    using System;

    interface IBehavior
    {
        void Invoke(BehaviorContext context, Action next);
    }
}