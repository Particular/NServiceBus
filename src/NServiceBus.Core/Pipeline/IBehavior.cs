namespace NServiceBus.Pipeline
{
    using System;


    interface IBehavior<T> : IBehavior where T : BehaviorContext
    {
        void Invoke(T context, Action next);
    }

    interface IBehavior
    {
        
    }
}