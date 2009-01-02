using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace V2.Messages
{
    public interface SomethingHappened : V1.Messages.SomethingHappened
    {
        string MoreInfo { get; set; }
    }
}
