namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Janitor;
    using NServiceBus.Pipeline;

    class BehaviorChain : IDisposable
    {
        public BehaviorChain(IEnumerable<BehaviorInstance> behaviorList, Dictionary<Type, string> lookupSteps, BusNotifications notifications)
        {
            this.lookupSteps = lookupSteps;
            this.notifications = notifications;

            itemDescriptors = behaviorList.ToArray();
        }

        public async Task Invoke(BehaviorContextStacker contextStacker)
        {
            var contextStacker = context.Builder.Build<BehaviorContextStacker>();
            
            var outerPipe = false;

            try
            {
                if (!context.TryGet(out diagnostics))
                {
                    outerPipe = true;
                    diagnostics = new PipelineDiagnostics();
                    context.Set(diagnostics);
                    notifications.Pipeline.InvokeReceiveStarted(diagnostics.StepsDiagnostics);
                }

                await InvokeNext(context, contextStacker, 0);

                if (outerPipe)
                {
                    diagnostics.StepsDiagnostics.OnCompleted();
                }
            }
            catch (Exception ex)
            {
                if (outerPipe)
                {
                    diagnostics.StepsDiagnostics.OnError(ex);
                }

                throw;
            }
            finally
            {
                if (outerPipe)
                {
                    context.Remove<PipelineDiagnostics>();
                }
            }
        }

        public void Dispose()
        {
            
        }

        async Task<BehaviorContext> InvokeNext(BehaviorContext context, BehaviorContextStacker contextStacker, int currentIndex)
        {
            Guard.AgainstNull("context", context);

            if (currentIndex == itemDescriptors.Length)
            {
                return context;
            }

            var behavior = itemDescriptors[currentIndex];
            var stepEnded = new Observable<StepEnded>();
            contextStacker.Push(context);
            try
            {
                diagnostics.StepsDiagnostics.OnNext(new StepStarted(lookupSteps[behavior.Type], behavior.Type, stepEnded));

                var duration = Stopwatch.StartNew();

                BehaviorContext innermostContext = null;
                await behavior.Invoke(context, async newContext =>
                {
                    duration.Stop();
                    innermostContext = await InvokeNext(newContext, contextStacker, currentIndex + 1);
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
        BusNotifications notifications;
        [SkipWeaving]
        BehaviorInstance[] itemDescriptors;
        Dictionary<Type, string> lookupSteps;
        PipelineDiagnostics diagnostics;
    }
}