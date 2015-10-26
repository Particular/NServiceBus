namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;

    class BehaviorContextStacker
    {
        public BehaviorContextStacker(BehaviorContext rootContext)
        {
            this.rootContext = rootContext;
        }
        
        /// <summary>
        /// The current <see cref="BehaviorContext"/> at the top of the stack.
        /// </summary>
        BehaviorContext Current
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
        /// Retrieves either the <see cref="Current"/> context or, of it is null, the root context.
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
        
        Stack<BehaviorContext> behaviorContextStack = new Stack<BehaviorContext>();
        BehaviorContext rootContext;
    }
}