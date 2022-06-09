namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;

    class BatchToDispatchConnector : StageConnector<IBatchDispatchContext, IDispatchContext>
    {
        public override async Task Invoke(IBatchDispatchContext context, Func<IDispatchContext, Task> stage)
        {
            var activityLinks = context.Operations.Select(o =>
            {
                var operationActivityContext = ActivityContext.Parse(o.Properties.TraceParent, null);
                return new ActivityLink(operationActivityContext);
            });
            //TODO do we need this activity at all?
            using var activity = ActivitySources.Main.StartActivity(name: ActivityNames.MessageDispatchActivityName, links: activityLinks, kind: ActivityKind.Producer);
            if (activity != null)
            {
                activity.DisplayName = "batch dispatch";
            }

            await stage(this.CreateDispatchContext(context.Operations, context)).ConfigureAwait(false);
        }
    }
}