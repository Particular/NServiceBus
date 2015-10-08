﻿namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using Contexts;
    using ObjectBuilder;

    /// <summary>
    ///     A stack of <see cref="BehaviorContext" />s.
    /// </summary>
    class BehaviorContextStacker : IDisposable
    {
        public BehaviorContextStacker(BehaviorContext rootContext)
        {
            this.rootContext = rootContext;
        }

        public BehaviorContextStacker(IBuilder builder)
            : this(new RootContext(builder))
        {
        }

        /// <summary>
        ///     The current <see cref="BehaviorContext" /> at the top of the stack.
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
        ///     <see cref="IDisposable.Dispose" />.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        ///     Retrieves either the <see cref="Current" /> context or, of it is null, the root context.
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
        ///     Pushes a new <see cref="BehaviorContext" /> onto the stack.
        /// </summary>
        public void Push(BehaviorContext item)
        {
            behaviorContextStack.Push(item);
        }

        /// <summary>
        ///     Removes the top <see cref="BehaviorContext" /> from the stack.
        /// </summary>
        public void Pop()
        {
            behaviorContextStack.Pop();
        }

        Stack<BehaviorContext> behaviorContextStack = new Stack<BehaviorContext>();
        BehaviorContext rootContext;
    }
}