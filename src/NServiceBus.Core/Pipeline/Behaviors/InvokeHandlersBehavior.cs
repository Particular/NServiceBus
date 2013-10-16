namespace NServiceBus.Pipeline.Behaviors
{
    using System;

    /// <summary>
    /// invoke handler'n'stuff
    /// </summary>
    class InvokeHandlersBehavior : IBehavior
    {
        public IBehavior Next { get; set; }
        public void Invoke(BehaviorContext context)
        {
            throw new NotImplementedException();
        }
    }
}