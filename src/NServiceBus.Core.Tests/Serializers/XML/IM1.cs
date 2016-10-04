namespace NServiceBus.Serializers.XML
{
    using System;

    public class IM1 : IMessage
    {
        public float Age { get; set; }
        public int Int { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public Uri Uri { get; set; }
        public Risk Risk { get; set; }
    }
}
