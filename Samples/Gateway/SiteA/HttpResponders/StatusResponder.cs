namespace SiteA.HttpResponders
{
    using System.Net;
    using NServiceBus;
    using NServiceBus.Gateway.Channels.Http;
    using NServiceBus.ObjectBuilder;

    public class StatusResponder : IHandleGatewayGets
    {
        public void Handle(HttpListenerContext ctx)
        {
            ctx.Response.StatusCode = 200;
            var response = string.Format("<html><body>EndpointName:{0} - Status: Ok</body></html>", Configure.EndpointName);

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(response);

            ctx.Response.ContentType = "text/html";
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.OutputStream.Close();
        }
    }

    public class Bootstrapper : IWantCustomInitialization
    {

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<StatusResponder>(DependencyLifecycle.InstancePerCall);
        }
    }
}