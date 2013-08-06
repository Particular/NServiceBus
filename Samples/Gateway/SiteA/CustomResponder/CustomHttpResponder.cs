namespace SiteA.CustomResponder
{
    using System.Linq;
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

                builder.BuildAll<IEndpointInspector>().ToList()
                    .ForEach(inspector =>
                    {
                        response += "<div>" + inspector.GetStatusAsHtml() + "</div>";
                    });

                ctx.Response.ContentType = "text/html";
                ctx.Response.Close(System.Text.Encoding.UTF8.GetBytes(response + "</body></html>"), true);

            }
            else
            {
                //knock yourself out
            }
        }
    }

    class ResponderInstaller : INeedInitialization
    {
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<CustomHttpResponder>(DependencyLifecycle.InstancePerCall);

            //register all the inspectors in the container
            Configure.TypesToScan.Where(t=>typeof(IEndpointInspector).IsAssignableFrom(t) && !t.IsInterface).ToList()
                .ForEach(inspector=> Configure.Instance.Configurer.ConfigureComponent(inspector,
                                                                                      DependencyLifecycle.InstancePerCall));
        }
    }

    public interface IEndpointInspector
    {
        string GetStatusAsHtml();
    }
}