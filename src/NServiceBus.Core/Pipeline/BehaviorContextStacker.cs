namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Janitor;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline.Contexts;

    class BehaviorContextStacker : IDisposable
    {
        public BehaviorContextStacker(IBuilder rootBuilder)
        {
            this.rootBuilder = rootBuilder;
            rootContext = new RootContext(rootBuilder);
        }

        public BehaviorContext Root
        {
            get { return rootContext; }
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

        public BehaviorContext GetCurrentOrRootContext()
        {
            var current = Current;

            if (current != null)
            {
                return current;
            }
            return rootContext;
        }

        public void Push(BehaviorContext item)
        {
            behaviorContextStack.Value.Push(item);
        }

        public void Pop()
        {
            behaviorContextStack.Value.Pop();
        }

        public void Dispose()
        {
        }

        [SkipWeaving]
        readonly IBuilder rootBuilder;
        //until we get the internal container going we
        ThreadLocal<Stack<BehaviorContext>> behaviorContextStack = new ThreadLocal<Stack<BehaviorContext>>(() => new Stack<BehaviorContext>());
        RootContext rootContext;
    }
}