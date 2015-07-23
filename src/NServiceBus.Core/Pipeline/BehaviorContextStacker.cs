namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// A stack of <see cref="BehaviorContext"/>s.
    /// </summary>
    public class BehaviorContextStacker : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of <see cref="BehaviorContextStacker"/>.
        /// </summary>
        public BehaviorContextStacker(IBuilder rootBuilder)
        {
            rootContext = new RootContext(rootBuilder);
        }

        /// <summary>
        /// The root <see cref="BehaviorContext"/> for this stack.
        /// </summary>
        public BehaviorContext Root
        {
            get { return rootContext; }
        }

        /// <summary>
        /// The current <see cref="BehaviorContext"/> at the top of the stack.
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
        /// Retrieves either the <see cref="Current"/> context or, of it is null, the <see cref="Root"/> context.
        /// </summary>
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
        /// Pushes a new <see cref="BehaviorContext"/> onto the stack.
        /// </summary>
        public void Push(BehaviorContext item)
        {
            behaviorContextStack.Push(item);
        }

        /// <summary>
        /// Removes the top <see cref="BehaviorContext"/> from the stack.
        /// </summary>
        public void Pop()
        {
            behaviorContextStack.Pop();
        }

        /// <summary>
        /// <see cref="IDisposable.Dispose"/>.
        /// </summary>
        public void Dispose()
        {
        }

        Stack<BehaviorContext> behaviorContextStack = new Stack<BehaviorContext>();
        RootContext rootContext;
    }
}