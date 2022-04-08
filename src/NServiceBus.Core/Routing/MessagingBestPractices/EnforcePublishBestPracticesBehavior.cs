﻿namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Pipeline;

    class EnforcePublishBestPracticesBehavior : IBehavior<IOutgoingPublishContext, IOutgoingPublishContext>
    {
        public EnforcePublishBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public Task Invoke(IOutgoingPublishContext context, Func<IOutgoingPublishContext, Task> next)
        {
            if (!context.Extensions.TryGet(ContextBag.GetPrefixedKey<EnforceBestPracticesOptions>(context.MessageId), out EnforceBestPracticesOptions options)
                || options.Enabled)
            {
                validations.AssertIsValidForPubSub(context.Message.MessageType);
            }

            return next(context);
        }

        readonly Validations validations;
    }
}