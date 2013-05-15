namespace NServiceBus.Gateway.Receiving
{
    using System;

    public class ChannelException : Exception
    {
        public int StatusCode { get; private set; }

        public ChannelException(int statusCode,string message):base(message)
        {
            StatusCode = statusCode;
        }
    }
}