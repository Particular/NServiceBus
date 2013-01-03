namespace MyMessages.RequestResponse
{
    using NServiceBus;

    public class MyResponse : IMessage
    {
        public string ResponseData { get; set; }
    }
}