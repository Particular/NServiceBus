namespace NServiceBus.Gateway.Channels.Http
{
    using System.Net;

    public class DefaultResponder : IHttpResponder
    {
        public void Handle(HttpListenerContext ctx)
        {
            ctx.Response.StatusCode = 200;
            
            var response = string.Format("<html><body>EndpointName:{0} - Status: Ok</body></html>", Configure.EndpointName);

            ctx.Response.ContentType = "text/html";
            ctx.Response.Close(System.Text.Encoding.UTF8.GetBytes(response), true);
        }
    }
}