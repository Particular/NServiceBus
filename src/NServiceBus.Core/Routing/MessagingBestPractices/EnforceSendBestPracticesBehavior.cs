#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class EnforceSendBestPracticesBehavior(Validations validations) : IBehavior<IOutgoingSendContext, IOutgoingSendContext>
{
    public Task Invoke(IOutgoingSendContext context, Func<IOutgoingSendContext, Task> next)
    {
        if (!context.Extensions.TryGet<EnforceBestPracticesOptions>(out var options) || options.Enabled)
        {
            validations.AssertIsValidForSend(context.Message.MessageType);
        }

        return next(context);
    }
}