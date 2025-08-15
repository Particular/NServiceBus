#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class EnforceSubscribeBestPracticesBehavior(Validations validations) : IBehavior<ISubscribeContext, ISubscribeContext>
{
    public Task Invoke(ISubscribeContext context, Func<ISubscribeContext, Task> next)
    {
        if (!context.Extensions.TryGet<EnforceBestPracticesOptions>(out var options) || options.Enabled)
        {
            foreach (var eventType in context.EventTypes)
            {
                validations.AssertIsValidForPubSub(eventType);
            }
        }

        return next(context);
    }
}