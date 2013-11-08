namespace NServiceBus.Pipeline
{
    using System;


    interface IBehavior<T> where T : BehaviorContext
    {
        void Invoke(T context, Action next);
    }
}