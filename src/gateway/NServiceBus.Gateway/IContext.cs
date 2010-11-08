using System;
using System.IO;
using System.Collections.Specialized;

namespace NServiceBus.Gateway
{
    public interface IContext
    {
        int RequestContentLength { get; }
        Stream RequestInputStream { get; }
        Stream ResponseOutputStream { get; }
        NameValueCollection RequestHeaders { get; }

        void EndResponse();
    }
}
