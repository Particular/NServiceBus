using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NServiceBus.ObjectBuilder.Common
{
    public interface IContainInternalBuilder
    {
        IBuilderInternal Builder { get; set; }
    }
}
