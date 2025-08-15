#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class EnforceReplyBestPracticesBehavior(Validations validations) : IBehavior<IOutgoingReplyContext, IOutgoingReplyContext>
{
    public Task Invoke(IOutgoingReplyContext context, Func<IOutgoingReplyContext, Task> next)
    {
        if (!context.Extensions.TryGet<EnforceBestPracticesOptions>(out var options) || options.Enabled)
        {
            validations.AssertIsValidForReply(context.Message.MessageType);
        }

        return next(context);
    }
}