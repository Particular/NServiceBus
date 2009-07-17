using System;

namespace NServiceBus.Host.Tests
{
    public class ServerEndpoint:IMessageEndpoint
    {
        public void OnStart()
        {
            throw new NotImplementedException();
        }

        public void OnStop()
        {
            throw new NotImplementedException();
        }
    }
}