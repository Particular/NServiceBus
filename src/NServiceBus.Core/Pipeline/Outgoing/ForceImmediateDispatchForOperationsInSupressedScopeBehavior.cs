namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Logging;
    using Pipeline;

    [ObsoleteEx(RemoveInVersion = "7")]
    class ForceImmediateDispatchForOperationsInSuppressedScopeBehavior : IBehavior<IRoutingContext, IRoutingContext>
    {
        public Task Invoke(IRoutingContext context, Func<IRoutingContext, Task> next)
        {
            var state = context.Extensions.GetOrCreate<InvokeHandlerTerminator.State>();

            //if there is no scope here the user must have suppressed it
            if (state.ScopeWasPresent && Transaction.Current == null)
            {
                var dispatchState = context.Extensions.GetOrCreate<RoutingToDispatchConnector.State>();

                if (!dispatchState.ImmediateDispatch)
                {
                    Logger.Warn(scopeWarning);

                    dispatchState.ImmediateDispatch = true;
                }
            }

            return next(context);
        }

        static string scopeWarning = @"
Suppressed ambient transaction detected when requesting the outgoing operation. 
Support for this behavior is deprecated and will be removed in Version 7. The new api for requesting immediate dispatch is:

var options = new Send|Publish|ReplyOptions();

options.RequireImmediateDispatch();

session.Send|Publish|Reply(new MyMessage(), options)
";

        static ILog Logger = LogManager.GetLogger<ForceImmediateDispatchForOperationsInSuppressedScopeBehavior>();
    }
}