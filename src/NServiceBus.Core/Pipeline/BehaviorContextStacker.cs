namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// 
    /// </summary>
    public class BehaviorContextStacker : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootBuilder"></param>
        public BehaviorContextStacker(IBuilder rootBuilder)
        {
            rootContext = new RootContext(rootBuilder);
        }

        /// <summary>
        /// 
        /// </summary>
        public BehaviorContext Root
        {
            get { return rootContext; }
        }

        /// <summary>
        /// 
        /// </summary>
        public BehaviorContext Current
        {
            get
            {
                if (behaviorContextStack.Count == 0)
                {
                    return null;
                }

                return behaviorContextStack.Peek();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public BehaviorContext GetCurrentOrRootContext()
        {
            var current = Current;

            if (current != null)
            {
                return current;
            }
            return rootContext;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public void Push(BehaviorContext item)
        {
            behaviorContextStack.Push(item);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Pop()
        {
            behaviorContextStack.Pop();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
        }

        Stack<BehaviorContext> behaviorContextStack = new Stack<BehaviorContext>();
        RootContext rootContext;
    }
}