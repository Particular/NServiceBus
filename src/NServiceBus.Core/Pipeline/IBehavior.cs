namespace NServiceBus.Pipeline
{
    using System;
    using System.ComponentModel;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IBehavior<T> where T : BehaviorContext
    {
        void Invoke(T context, Action next);
    }

}