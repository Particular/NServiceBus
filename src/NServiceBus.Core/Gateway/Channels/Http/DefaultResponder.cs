namespace NServiceBus.Gateway.Channels.Http
{
    using System.Net;
    using System.Text;

    public class DefaultResponder : IHttpResponder
    {
        public void Handle(HttpListenerContext ctx)
        {
            ctx.Response.StatusCode = 200;

            var response = string.Format("<html><body>EndpointName:{0} - Status: Ok</body></html>",EndpointName);

            ctx.Response.ContentType = "text/html";

            if (ctx.Request.HttpMethod == WebRequestMethods.Http.Head)
            {
                ctx.Response.Close();
                return;
            }

            ctx.Response.Close(Encoding.UTF8.GetBytes(response), true);
        }

        public string EndpointName { get; set; }
    }
}