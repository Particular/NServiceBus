namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Janitor;
    using NServiceBus.Pipeline;

    class BehaviorChain : IDisposable
    {
        public BehaviorChain(IEnumerable<BehaviorInstance> behaviorList, BehaviorContext context, Dictionary<Type, string> lookupSteps, BusNotifications notifications)
        {
            this.context = context;
            this.lookupSteps = lookupSteps;
            this.notifications = notifications;

            itemDescriptors = behaviorList.ToArray();
        }

        public BehaviorContext Invoke(BehaviorContextStacker contextStacker)
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

                var result = InvokeNext(context, contextStacker, 0);

                if (outerPipe)
                {
                    steps.OnCompleted();
                }

                return result;
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

        public void Dispose()
        {
            
        }

        BehaviorContext InvokeNext(BehaviorContext context, BehaviorContextStacker contextStacker, int currentIndex)
        {
            Guard.AgainstNull(context, "context");

            if (currentIndex == itemDescriptors.Length)
            {
                return context;
            }

            var behavior = itemDescriptors[currentIndex];
            var stepEnded = new Observable<StepEnded>();
            contextStacker.Push(context);
            try
            {
                steps.OnNext(new StepStarted(lookupSteps[behavior.Type], behavior.Type, stepEnded));

                var instance = behavior.GetInstance();

                var duration = Stopwatch.StartNew();

                BehaviorContext innermostContext = null;
                behavior.Invoke(instance, context, newContext =>
                {
                    duration.Stop();
                    innermostContext = InvokeNext(newContext, contextStacker, currentIndex + 1);
                    duration.Start();
                });

                duration.Stop();

                stepEnded.OnNext(new StepEnded(duration.Elapsed));
                stepEnded.OnCompleted();

                return innermostContext ?? context;
            }
            catch (Exception ex)
            {
                stepEnded.OnError(ex);

                throw;
            }
            finally
            {
                contextStacker.Pop();
            }
        }

        [SkipWeaving]
        readonly BusNotifications notifications;
        readonly BehaviorContext context;
        readonly BehaviorInstance[] itemDescriptors;
        readonly Dictionary<Type, string> lookupSteps;
        Observable<StepStarted> steps;
    }
}