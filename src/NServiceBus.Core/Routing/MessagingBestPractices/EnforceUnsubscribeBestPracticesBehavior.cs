#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class EnforceUnsubscribeBestPracticesBehavior(Validations validations) : IBehavior<IUnsubscribeContext, IUnsubscribeContext>
{
    public Task Invoke(IUnsubscribeContext context, Func<IUnsubscribeContext, Task> next)
    {
        if (!context.Extensions.TryGet<EnforceBestPracticesOptions>(out var options) || options.Enabled)
        {
            validations.AssertIsValidForPubSub(context.EventType);
        }

        return next(context);
    }
}