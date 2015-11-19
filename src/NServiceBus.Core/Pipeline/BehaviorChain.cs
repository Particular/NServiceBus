﻿namespace NServiceBus
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

        public async Task Invoke(BehaviorContext context)
        {
            var outerPipe = false;
            try
            {
                if (!context.Extensions.TryGet(out diagnostics))
                {
                    outerPipe = true;
                    diagnostics = new PipelineDiagnostics();
                    context.Extensions.Set(diagnostics);
                    notifications.Pipeline.InvokeReceiveStarted(diagnostics.StepsDiagnostics);
                }

                await InvokeNext(context, 0).ConfigureAwait(false);

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
                    context.Extensions.Remove<PipelineDiagnostics>();
                }
            }
        }

        public void Dispose()
        {

        }

        async Task<BehaviorContext> InvokeNext(BehaviorContext context, int currentIndex)
        {
            Guard.AgainstNull(nameof(context), context);

            if (currentIndex == itemDescriptors.Length)
            {
                return context;
            }

            var behavior = itemDescriptors[currentIndex];
            var stepEnded = new Observable<StepEnded>();
            try
            {
                diagnostics.StepsDiagnostics.OnNext(new StepStarted(lookupSteps[behavior.Type], behavior.Type, stepEnded));

                var duration = Stopwatch.StartNew();

                BehaviorContext innermostContext = null;
                await behavior.Invoke(context, async newContext =>
                {
                    duration.Stop();
                    innermostContext = await InvokeNext(newContext, currentIndex + 1).ConfigureAwait(false);
                    duration.Start();
                }).ConfigureAwait(false);

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
        }

        [SkipWeaving]
        BusNotifications notifications;
        [SkipWeaving]
        BehaviorInstance[] itemDescriptors;
        Dictionary<Type, string> lookupSteps;
        PipelineDiagnostics diagnostics;
    }
}