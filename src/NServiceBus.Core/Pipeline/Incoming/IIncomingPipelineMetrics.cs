namespace NServiceBus;

using System;
using Pipeline;

interface IIncomingPipelineMetrics
{
    void AddDefaultIncomingPipelineMetricTags(IncomingPipelineMetricTags incomingPipelineMetricsTags);
    void RecordMessageSuccessfullyProcessed(ITransportReceiveContext context, IncomingPipelineMetricTags incomingPipelineMetricTags);
    void RecordMessageProcessingFailure(IncomingPipelineMetricTags incomingPipelineMetricTags, Exception error);
    void RecordFetchedMessage(IncomingPipelineMetricTags incomingPipelineMetricTags);
    void RecordSuccessfulMessageHandlerTime(IInvokeHandlerContext invokeHandlerContext, TimeSpan elapsed);
    void RecordFailedMessageHandlerTime(IInvokeHandlerContext invokeHandlerContext, TimeSpan elapsed, Exception error);
    void RecordImmediateRetry(IRecoverabilityContext recoverabilityContext);
    void RecordDelayedRetry(IRecoverabilityContext recoverabilityContext);
    void RecordSendToErrorQueue(IRecoverabilityContext recoverabilityContext);
}