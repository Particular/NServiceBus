namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// Special terminator behavior that eliminates the need for null checks all the way through the pipeline
    /// </summary>
    class Terminator : IBehavior
    {
        public IBehavior Next
        {
            get { throw new InvalidOperationException("Can't get next on a terminator - this behavior terminates the pipeline"); }
            set { throw new InvalidOperationException("Can't set next on a terminator - this behavior terminates the pipeline"); }
        }

        public void Invoke(IBehaviorContext context)
        {
            // noop :)
        }
    }
}