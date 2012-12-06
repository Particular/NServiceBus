using System;
using NServiceBus;

namespace MyMessages
{
    [Serializable]
    public class ThumbNailCreated : IMessage
    {
        public string ThumbNailUrl { get; set; }
        public Size Size { get; set; }
    }
}