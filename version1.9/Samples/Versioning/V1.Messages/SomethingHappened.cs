using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus;

namespace V1.Messages
{
    public interface SomethingHappened : IMessage
    {
        int SomeData { get; set; }
    }
}
