namespace NServiceBus.Core.Tests.Handler
{
    using ConventionBasedMessages;

    public class ConventionBasedHandler: IHandleMessages<MyMessage>
    {
        public void Handle(MyMessage message)
        {
            throw new System.NotImplementedException();
        }
    }
}
