namespace NServiceBus.Gateway.Channels.Http
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class HttpChannelException:Exception
    {
        public int StatusCode { get; private set; }

        public HttpChannelException(int statusCode,string message):base(message)
        {
            StatusCode = statusCode;
        }
        protected HttpChannelException(SerializationInfo info, StreamingContext context)
        {
            StatusCode = info.GetInt32("StatusCode");
        }
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("StatusCode", StatusCode);
        }
    }
}