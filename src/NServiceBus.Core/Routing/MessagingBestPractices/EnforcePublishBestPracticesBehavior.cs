#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class EnforcePublishBestPracticesBehavior(Validations validations) : IBehavior<IOutgoingPublishContext, IOutgoingPublishContext>
{
    public Task Invoke(IOutgoingPublishContext context, Func<IOutgoingPublishContext, Task> next)
    {
        if (!context.Extensions.TryGet<EnforceBestPracticesOptions>(out var options) || options.Enabled)
        {
            validations.AssertIsValidForPubSub(context.Message.MessageType);
        }

        return next(context);
    }
}