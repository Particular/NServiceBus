﻿namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    internal class BehaviorContextStacker : IDisposable
    {
        public BehaviorContext Current
        {
            get { return behaviorContextStack.Value.Peek(); }
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

        ThreadLocal<Stack<BehaviorContext>> behaviorContextStack = new ThreadLocal<Stack<BehaviorContext>>(() => new Stack<BehaviorContext>());
    }
}