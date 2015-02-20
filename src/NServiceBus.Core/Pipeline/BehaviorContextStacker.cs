namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using System.Threading;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline.Contexts;

    class BehaviorContextStacker
    {
        public BehaviorContextStacker(IBuilder rootBuilder)
        {
            this.rootBuilder = rootBuilder;
        }

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

        public BehaviorContext GetCurrentContext()
        {
            var current = Current;

            if (current != null)
            {
                return current;
            }

            Push(new RootContext(rootBuilder));

            return Current;
        }

        public void Push(BehaviorContext item)
        {
            behaviorContextStack.Value.Push(item);
        }

        public void Pop()
        {
            behaviorContextStack.Value.Pop();
        }

        readonly IBuilder rootBuilder;

        //until we get the internal container going we
        ThreadLocal<Stack<BehaviorContext>> behaviorContextStack = new ThreadLocal<Stack<BehaviorContext>>(() => new Stack<BehaviorContext>());

        
    }
}