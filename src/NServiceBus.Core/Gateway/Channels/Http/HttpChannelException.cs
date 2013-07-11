namespace NServiceBus.Gateway.Channels.Http
{
    using System;

    public class HttpChannelException:Exception
    {
        public int StatusCode { get; private set; }

        public HttpChannelException(int statusCode,string message):base(message)
        {
            StatusCode = statusCode;
        }
    }
}