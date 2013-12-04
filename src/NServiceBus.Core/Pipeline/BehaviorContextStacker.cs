namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    class BehaviorContextStacker : IDisposable
    {
        public BehaviorContext Current
        {
            get
            {
                if (behaviorContextStack.Value.Count == 0)
                {
                    return null;
                }

                return behaviorContextStack.Value.Peek();
            }
        }

        public void Dispose()
        {
            //Injected
        }

        public void Push(BehaviorContext item)
        {
            behaviorContextStack.Value.Push(item);
        }

        public void Pop()
        {
            behaviorContextStack.Value.Pop();
        }

        //until we get the internal container going we
        ThreadLocal<Stack<BehaviorContext>> behaviorContextStack = new ThreadLocal<Stack<BehaviorContext>>(() => new Stack<BehaviorContext>());
    }
}