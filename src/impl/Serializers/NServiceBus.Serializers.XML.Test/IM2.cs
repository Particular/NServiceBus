using System;
using System.Collections.Generic;

namespace NServiceBus.Serializers.XML.Test
{
    public interface IM2 : IM1
    {
        Guid Id { get; set; }
        IM1 Parent { get; set; }
        List<IM1> Names { get; set; }
        SomeEnum Some { get; set; }
        DateTime Start { get; set; }
        TimeSpan Duration { get; set; }
        DateTimeOffset Offset { get; set; }
        IM1[] MoreNames { get; set; }
        object State { get; set; }
        IDictionary<string, string> Lookup { get; set; }
    }
}
