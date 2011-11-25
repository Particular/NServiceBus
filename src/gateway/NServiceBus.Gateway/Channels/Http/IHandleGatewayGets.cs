namespace NServiceBus.Gateway.Channels.Http
{
    using System.Net;

    public interface IHandleGatewayGets
    {
        void Handle(HttpListenerContext ctx);
    }
}