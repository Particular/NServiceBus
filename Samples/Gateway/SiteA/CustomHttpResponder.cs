namespace SiteA
{
    using System.Net;
    using NServiceBus;
    using NServiceBus.Gateway.Channels.Http;
    using NServiceBus.ObjectBuilder;

    public class CustomHttpResponder : IHttpResponder
    {
        IBuilder builder;

        public CustomHttpResponder(IBuilder builder)
        {
            this.builder = builder;
        }

        public void Handle(HttpListenerContext ctx)
        {
            if(ctx.Request.HttpMethod == "GET")
            {
                ctx.Response.StatusCode = 200;

                var response = string.Format("<html><body><div><h1>Welcome to {0}</h1></div>", Configure.EndpointName);
                ctx.Response.ContentType = "text/html";
                ctx.Response.Close(System.Text.Encoding.UTF8.GetBytes(response + "</body></html>"), true);

            }
            else
            {
                //knock yourself out
            }
        }
    }
}