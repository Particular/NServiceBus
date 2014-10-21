namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using NServiceBus.Pipeline;

    class BehaviorChain<T> where T : BehaviorContext
    {
        // ReSharper disable StaticFieldInGenericType

        public BehaviorChain(IEnumerable<Type> behaviorList, T context, PipelineExecutor pipelineExecutor, BusNotifications notifications)
        {
            context.SetChain(this);
            this.context = context;
            this.notifications = notifications;
            foreach (var behaviorType in behaviorList)
            {
                itemDescriptors.Enqueue(behaviorType);
            }

            PopulateLookupTable(pipelineExecutor);
        }

        static void PopulateLookupTable(PipelineExecutor executor)
        {
            if (lookupSteps == null)
            {
                lock (lockObj)
                {
                    if (lookupSteps == null)
                    {
                        lookupSteps = executor.Incoming.Concat(executor.Outgoing).ToDictionary(rs => rs.BehaviorType);
                    }
                }
            }
        }

        public void Invoke()
        {
            Stopwatch duration = null;
            var pipeId = Guid.NewGuid().ToString();

            try
            {
                notifications.Pipeline.InvokePipeStarted(new PipeStarted
                {
                    PipeId = pipeId
                });
                duration = Stopwatch.StartNew();
                InvokeNext(context, pipeId);
            }
            finally
            {
                var elapsed = TimeSpan.Zero;
                if (duration != null)
                {
                    duration.Stop();
                    elapsed = duration.Elapsed;
                }

                notifications.Pipeline.InvokePipeEnded(new PipeEnded
                {
                    PipeId = pipeId,
                    Duration = elapsed
                });
            }
        }

        public void TakeSnapshot()
        {
            snapshots.Push(new Queue<Type>(itemDescriptors));
        }

        public void DeleteSnapshot()
        {
            itemDescriptors = new Queue<Type>(snapshots.Pop());
        }

        void InvokeNext(T context, string pipeId)
        {
            if (itemDescriptors.Count == 0)
            {
                return;
            }

            var behaviorType = itemDescriptors.Dequeue();
            Stopwatch duration = null;
            try
            {
                var instance = (IBehavior<T>) context.Builder.Build(behaviorType);

                notifications.Pipeline.InvokeStepStarted(new StepStarted
                {
                    Behavior = behaviorType,
                    StepId = lookupSteps[behaviorType].StepId,
                    PipeId = pipeId
                });

                duration = Stopwatch.StartNew();

                instance.Invoke(context, () => InvokeNext(context, pipeId));
            }
            finally
            {
                var elapsed = TimeSpan.Zero;
                if (duration != null)
                {
                    duration.Stop();
                    elapsed = duration.Elapsed;
                }

                notifications.Pipeline.InvokeStepEnded(new StepEnded
                {
                    StepId = lookupSteps[behaviorType].StepId,
                    PipeId = pipeId,
                    Duration = elapsed
                });
            }
        }

        static Dictionary<Type, RegisterStep> lookupSteps;
        static object lockObj = new object();
        readonly BusNotifications notifications;
        T context;
        Queue<Type> itemDescriptors = new Queue<Type>();
        Stack<Queue<Type>> snapshots = new Stack<Queue<Type>>();
    }
}