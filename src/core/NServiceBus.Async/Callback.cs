using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus.Async
{
    public delegate void CompletionCallback(int errorCode, object state);
}
