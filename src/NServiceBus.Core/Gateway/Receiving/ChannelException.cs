namespace NServiceBus.Gateway.Receiving
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class ChannelException : Exception
    {
        public ChannelException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        protected ChannelException(SerializationInfo info, StreamingContext context)
        {
            StatusCode = info.GetInt32("StatusCode");
        }

        public int StatusCode { get; private set; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("StatusCode", StatusCode);
        }
    }
}