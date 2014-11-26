namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using NServiceBus.Pipeline;

    class BehaviorChain<T> where T : BehaviorContext
    {
        public BehaviorChain(IEnumerable<Type> behaviorList, T context, PipelineExecutor pipelineExecutor, BusNotifications notifications)
        {
            context.SetChain(this);
            this.context = context;
            this.notifications = notifications;
            foreach (var behaviorType in behaviorList)
            {
                itemDescriptors.Enqueue(behaviorType);
            }

            lookupSteps = pipelineExecutor.Incoming.Concat(pipelineExecutor.Outgoing).ToDictionary(rs => rs.BehaviorType);
        }
        
        public void Invoke()
        {
            var outerPipe = false;

            try
            {
                if (!context.TryGet("Diagnostics.Pipe", out steps))
                {
                    outerPipe = true;
                    steps = new Observable<StepStarted>();
                    context.Set("Diagnostics.Pipe", steps);
                    notifications.Pipeline.InvokeReceiveStarted(steps);
                }
            
                InvokeNext(context);

                if (outerPipe)
                {
                    steps.OnCompleted();
                }
            }
            catch (Exception ex)
            {
                if (outerPipe)
                {
                    steps.OnError(ex);
                }

                throw;
            }
            finally
            {
                if (outerPipe)
                {
                    context.Remove("Diagnostics.Pipe");
                }
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

        void InvokeNext(T context)
        {
            if (itemDescriptors.Count == 0)
            {
                return;
            }

            var behaviorType = itemDescriptors.Dequeue();
            var stepEnded = new Observable<StepEnded>();

            try
            {
                steps.OnNext(new StepStarted(lookupSteps[behaviorType].StepId, behaviorType, stepEnded));

                var instance = (IBehavior<T>) context.Builder.Build(behaviorType);

                var duration = Stopwatch.StartNew();

                instance.Invoke(context, () =>
                {
                    duration.Stop();
                    InvokeNext(context);
                    duration.Start();
                });

                duration.Stop();

                stepEnded.OnNext(new StepEnded(duration.Elapsed));
                stepEnded.OnCompleted();
            }
            catch (Exception ex)
            {
                stepEnded.OnError(ex);

                throw;
            }
        }

        readonly BusNotifications notifications;
        T context;
        Queue<Type> itemDescriptors = new Queue<Type>();
        Dictionary<Type, RegisterStep> lookupSteps;
        Stack<Queue<Type>> snapshots = new Stack<Queue<Type>>();
        Observable<StepStarted> steps;
    }
}