namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class BatchToDispatchConnector : StageConnector<IBatchDispatchContext, IDispatchContext>
    {
        readonly bool createDispatchSpan;

        public BatchToDispatchConnector(bool createDispatchSpan) => this.createDispatchSpan = createDispatchSpan;

        public override async Task Invoke(IBatchDispatchContext context, Func<IDispatchContext, Task> stage)
        {
            using var activity = CreateDispatchActivity(context);

            await stage(this.CreateDispatchContext(context.Operations, context)).ConfigureAwait(false);
        }

        Activity CreateDispatchActivity(IBatchDispatchContext context)
        {
            // The dispatch activity is optional for scenarios where a transport package or transport SDK creates spans that can't be directly connected to the send operation
            if (!createDispatchSpan)
            {
                return null;
            }

            // link to send operations
            var activityLinks = new List<ActivityLink>(context.Operations.Count);
            foreach (TransportOperation transportOperation in context.Operations)
            {
                if (transportOperation.Message.Headers.TryGetValue(Headers.DiagnosticsTraceParent, out var traceparent))
                {
                    var operationActivityContext = ActivityContext.Parse(traceparent, null);
                    activityLinks.Add(new ActivityLink(operationActivityContext));
                }
            }

            return ActivitySources.Main.StartActivity(name: ActivityNames.MessageDispatchActivityName, links: activityLinks, kind: ActivityKind.Producer);

        }
    }
}