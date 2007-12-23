using System;

namespace NServiceBus
{
    public interface ICallback
    {
        IAsyncResult Register(AsyncCallback callback, object state);
    }
}
