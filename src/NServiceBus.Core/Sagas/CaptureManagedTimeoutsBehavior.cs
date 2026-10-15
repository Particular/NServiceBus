//#nullable enable
//namespace NServiceBus;

//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using Pipeline;
//using Sagas;
//using Transport;

//public class CaptureManagedTimeoutsBehavior : IBehavior<InvokeHandlerContext, InvokeHandlerContext>
//{
//    public async Task Invoke(InvokeHandlerContext context, Func<InvokeHandlerContext, Task> next)
//    {
//        await next(context).ConfigureAwait(false);

//        var sagaInstanceState = context.Extensions.Get<ActiveSagaInstance>();
//        var pendingTransportOperations = context.Extensions.Get<PendingTransportOperations>();

//        var requestedTimeouts = pendingTransportOperations.Operations.Where(x => x.Message.Headers.ContainsKey(Headers.IsSagaTimeoutMessage));
//        foreach (var requestedTimeout in requestedTimeouts)
//        {
//            var timeoutId = Guid.NewGuid().ToString();
//            var triggerId = Guid.NewGuid().ToString();

//            var timeoutInfo = new TimeoutInfo()
//            {
//                State = requestedTimeout.Message.
//            };
//        }
//    }
//}