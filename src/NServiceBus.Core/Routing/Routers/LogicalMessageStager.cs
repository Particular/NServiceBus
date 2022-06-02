namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Pipeline;
    using Unicast.Queuing;

    static class LogicalMessageStager
    {
        public static async Task StageOutgoing(Func<IOutgoingLogicalMessageContext, Task> stager, IOutgoingLogicalMessageContext logicalMessageContext, Activity activity, string queueNotFoundMessage = null)
        {
            try
            {
                logicalMessageContext.Extensions.Set(DiagnosticsKeys.OutgoingActivityKey, activity);
                await stager(logicalMessageContext).ConfigureAwait(false);
            }
            catch (QueueNotFoundException qnfe)
            {
                var err = new Exception($"The destination queue '{qnfe.Queue}' could not be found. " +
                                        queueNotFoundMessage + "It may be the case that the given queue hasn't been created yet, or has been deleted.", qnfe);
                activity?.SetStatus(ActivityStatusCode.Error, err.Message);
                throw err;
            }
#pragma warning disable PS0019 // Do not catch Exception without considering OperationCanceledException - enriching and rethrowing
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
#pragma warning restore PS0019 // Do not catch Exception without considering OperationCanceledException

            //TODO should we stop the activity only once the message has been handed to the dispatcher?
            activity?.SetStatus(ActivityStatusCode.Ok); //Set activity state.
        }
    }
}