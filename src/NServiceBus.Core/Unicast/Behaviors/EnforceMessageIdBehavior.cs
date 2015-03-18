namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;

    class EnforceMessageIdBehavior : PhysicalMessageProcessingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            if (string.IsNullOrWhiteSpace(context.PhysicalMessage.Id))
            {     
                throw new MessageDeserializationException("Message without message id detected");    
            }

            next();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("EnforceMessageId", typeof(EnforceMessageIdBehavior), "Makes sure that the message pulled from the transport contains a message id")
            {
                InsertBeforeIfExists("SecondLevelRetries");
                InsertBeforeIfExists("FirstLevelRetries");
                InsertBeforeIfExists("ReceivePerformanceDiagnosticsBehavior");
            }
        }
    }
}