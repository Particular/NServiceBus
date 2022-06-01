namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;

    class BatchToDispatchConnector : StageConnector<IBatchDispatchContext, IDispatchContext>
    {
        public override Task Invoke(IBatchDispatchContext context, Func<IDispatchContext, Task> stage)
        {
            var activityLinks = context.Operations.Select(o =>
            {
                var operationActivityContext = ActivityContext.Parse(o.Properties.TraceParent, null);
                return new ActivityLink(operationActivityContext);
            });
            using var activity = ActivitySources.Main.StartActivity(name: "dispatching", links: activityLinks, kind: ActivityKind.Producer);
            return stage(this.CreateDispatchContext(context.Operations, context));
        }
    }
}