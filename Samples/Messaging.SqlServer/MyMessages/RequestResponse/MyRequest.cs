namespace MyMessages.RequestResponse
{
    using NServiceBus;

    public class MyRequest : IMessage
    {
        public string RequestData { get; set; }
    }
}