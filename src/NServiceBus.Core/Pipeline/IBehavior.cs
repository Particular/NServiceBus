namespace NServiceBus.Pipeline
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IBehavior<T> where T : BehaviorContext
    {
        void Invoke(T context, Action next);
    }

}