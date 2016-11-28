namespace NServiceBus.Serializers.XML.Test
{
    using System;
    using System.Collections.Generic;


    public class SecondSerializableMessage : FirstSerializableMessage
    {
        public Guid Id { get; set; }
        public List<FirstSerializableMessage> Names { get; set; }
        public FirstSerializableMessage Parent { get; set; }
        public SomeEnum Some { get; set; }
        public DateTime Start { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTimeOffset Offset { get; set; }
        public FirstSerializableMessage[] MoreNames { get; set; }
    }
}
