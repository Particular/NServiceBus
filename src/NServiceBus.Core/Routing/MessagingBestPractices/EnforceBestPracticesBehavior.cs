namespace NServiceBus
{
    using System;
    using NServiceBus.MessagingBestPractices;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;

    class EnforceBestPracticesBehavior : Behavior<OutgoingContext>
    {
        Validations validations;

        public EnforceBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public override void Invoke(OutgoingContext context, Action next)
        {
            Options options;

            if (!context.Extensions.TryGet(out options) || options.Enabled)
            {
                if (context.IsReply())
                {
                    validations.AssertIsValidForReply(context.MessageType);
                }
                if (context.IsPublish())
                {
                    validations.AssertIsValidForPubSub(context.MessageType);
                }
                if (context.IsSend())
                {
                    validations.AssertIsValidForSend(context.MessageType);
                }
            }
            
            next();
        }

        public class Options
        {
            public Options()
            {
                Enabled = true;
            }

            public bool Enabled { get; set; }
        }
    }
}