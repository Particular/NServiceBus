namespace NServiceBus.Core.Tests.x64
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
