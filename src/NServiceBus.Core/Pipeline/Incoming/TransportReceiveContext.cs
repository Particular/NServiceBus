namespace NServiceBus;

using System;
using System.Threading;
using Extensibility;
using Pipeline;
using Transport;

/// <summary>
/// Context containing a physical message.
/// </summary>
partial class TransportReceiveContext : PipelineRootContext, ITransportReceiveContext
{
    /// <summary>
    /// Creates a new transport receive context.
    /// </summary>
    public TransportReceiveContext(IServiceProvider serviceProvider, MessageOperations messageOperations, IPipelineCache pipelineCache, IncomingMessage receivedMessage, TransportTransaction transportTransaction, ContextBag parentContext, CancellationToken cancellationToken)
        : base(serviceProvider, messageOperations, pipelineCache, cancellationToken, parentContext)
    {
        Message = receivedMessage;
        Set(Message);
        Set(transportTransaction);
        //Hack to be able to get the tags in the recoverability pipeline
        _ = parentContext.GetOrCreate<IncomingPipelineMetricTags>();
    }

    /// <summary>
    /// The physical message being processed.
    /// </summary>
    public IncomingMessage Message { get; }
}