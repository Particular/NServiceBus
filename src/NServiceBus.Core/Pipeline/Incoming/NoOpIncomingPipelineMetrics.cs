namespace NServiceBus;

using System;
using Pipeline;

class NoOpIncomingPipelineMetrics : IIncomingPipelineMetrics
{
    public void AddDefaultIncomingPipelineMetricTags(IncomingPipelineMetricTags incomingPipelineMetricsTags) { }

    public void RecordMessageSuccessfullyProcessed(ITransportReceiveContext context, IncomingPipelineMetricTags incomingPipelineMetricTags) { }

    public void RecordMessageProcessingFailure(IncomingPipelineMetricTags incomingPipelineMetricTags, Exception error) { }

    public void RecordFetchedMessage(IncomingPipelineMetricTags incomingPipelineMetricTags) { }

    public void RecordSuccessfulMessageHandlerTime(IInvokeHandlerContext invokeHandlerContext, TimeSpan elapsed) { }

    public void RecordFailedMessageHandlerTime(IInvokeHandlerContext invokeHandlerContext, TimeSpan elapsed, Exception error) { }

    public void RecordImmediateRetry(IRecoverabilityContext recoverabilityContext) { }

    public void RecordDelayedRetry(IRecoverabilityContext recoverabilityContext) { }

    public void RecordSendToErrorQueue(IRecoverabilityContext recoverabilityContext) { }
}