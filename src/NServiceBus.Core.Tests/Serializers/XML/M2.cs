using System;
using System.Collections.Generic;

namespace NServiceBus.Serializers.XML.Test
{
    [Serializable]
    public class M2 : M1
    {
        public Guid Id { get; set; }
        public List<M1> Names { get; set; }
        public M1 Parent { get; set; }
        public SomeEnum Some { get; set; }
        public DateTime Start { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTimeOffset Offset { get; set; }
        public M1[] MoreNames { get; set; }
    }    
}
