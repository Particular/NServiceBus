using NServiceBus;

namespace Messages
{
    public class Hello : IMessage
    {
        public string Text { get; set; }
    }
}