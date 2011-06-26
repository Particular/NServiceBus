using System;
using System.Drawing.Imaging;
using NServiceBus;

namespace MyMessages
{
    [Serializable, TimeToBeReceived("00:05:00")]
    public class ImageUploaded : IMessage
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public DataBusProperty<byte[]> Image { get; set; }
    }
}
