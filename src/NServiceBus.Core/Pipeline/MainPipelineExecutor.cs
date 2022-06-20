namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Pipeline;
    using Transport;

    class MainPipelineExecutor : IPipelineExecutor
    {
        public MainPipelineExecutor(IServiceProvider rootBuilder, IPipelineCache pipelineCache, MessageOperations messageOperations, INotificationSubscriptions<ReceivePipelineCompleted> receivePipelineNotification, Pipeline<ITransportReceiveContext> receivePipeline)
        {
            this.rootBuilder = rootBuilder;
            this.pipelineCache = pipelineCache;
            this.messageOperations = messageOperations;
            this.receivePipelineNotification = receivePipelineNotification;
            this.receivePipeline = receivePipeline;
        }

        public async Task Invoke(MessageContext messageContext, CancellationToken cancellationToken = default)
        {
            var pipelineStartedAt = DateTimeOffset.UtcNow;

            using var activity = CreateIncomingActivity(messageContext);

            using (var childScope = rootBuilder.CreateScope())
            {
                var message = new IncomingMessage(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);

                ActivityDecorator.SetReceiveTags(activity, message);
                ActivityDecorator.SetHeaderTraceTags(activity, messageContext.Headers);

                var rootContext = new RootContext(childScope.ServiceProvider, messageOperations, pipelineCache, cancellationToken);
                rootContext.Extensions.Merge(messageContext.Extensions);

                var transportReceiveContext = new TransportReceiveContext(message, messageContext.TransportTransaction, rootContext);

                try
                {
                    await receivePipeline.Invoke(transportReceiveContext).ConfigureAwait(false);
                }
#pragma warning disable PS0019 // Do not catch Exception without considering OperationCanceledException - enriching and rethrowing
                catch (Exception ex)
#pragma warning restore PS0019 // Do not catch Exception without considering OperationCanceledException
                {
                    ex.Data["Message ID"] = message.MessageId;

                    if (message.NativeMessageId != message.MessageId)
                    {
                        ex.Data["Transport message ID"] = message.NativeMessageId;
                    }

                    ex.Data["Pipeline canceled"] = transportReceiveContext.CancellationToken.IsCancellationRequested;

                    // TODO: Add an explicit tag for operation canceled
                    ActivityDecorator.SetErrorStatus(activity, ex);
                    throw;
                }

                await receivePipelineNotification.Raise(new ReceivePipelineCompleted(message, pipelineStartedAt, DateTimeOffset.UtcNow), cancellationToken).ConfigureAwait(false);
            }

            activity?.SetStatus(ActivityStatusCode.Ok); //Set activity state.
        }

        static Activity CreateIncomingActivity(MessageContext context)
        {
            Activity activity;
            if (context.Extensions.TryGet(out Activity transportActivity)) // attach to transport span but link receive pipeline to send pipeline spa
            {
                ActivityLink[] links = null;
                if (context.Headers.TryGetValue(Headers.DiagnosticsTraceParent, out var sendSpanId) && sendSpanId == transportActivity.Id)
                {
                    //TODO do we need to pass the tracestate to the linked activityContext too?
                    if (ActivityContext.TryParse(sendSpanId, null, out var sendSpanContext))
                    {
                        links = new[] { new ActivityLink(sendSpanContext) };
                    }
                }

                activity = ActivitySources.Main.StartActivity(name: ActivityNames.IncomingMessageActivityName,
                    ActivityKind.Consumer, transportActivity.Context, links: links);
            }
            else if (context.Headers.TryGetValue(Headers.DiagnosticsTraceParent, out var sendSpanId)) // otherwise directly create child from logical send
            {
                var parent = ActivityContext.Parse(sendSpanId, null);
                activity = ActivitySources.Main.StartActivity(name: ActivityNames.IncomingMessageActivityName,
                    ActivityKind.Consumer, parent);
            }
            else // otherwise start new trace
            {
                activity = ActivitySources.Main.StartActivity(name: ActivityNames.IncomingMessageActivityName,
                    ActivityKind.Consumer);

            }

            ContextPropagation.PropagateContextFromHeaders(activity, context.Headers);

            return activity;
        }

        readonly IServiceProvider rootBuilder;
        readonly IPipelineCache pipelineCache;
        readonly MessageOperations messageOperations;
        readonly INotificationSubscriptions<ReceivePipelineCompleted> receivePipelineNotification;
        readonly Pipeline<ITransportReceiveContext> receivePipeline;
    }
}