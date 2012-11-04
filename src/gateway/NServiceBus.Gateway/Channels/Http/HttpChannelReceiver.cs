﻿namespace NServiceBus.Gateway.Channels.Http
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Web;
    using HeaderManagement;
    using log4net;
    using System.Net;
    using System.Text;
    using System.Threading;
    using Utils;

    public class HttpChannelReceiver:IChannelReceiver
    {
        public event EventHandler<DataReceivedOnChannelArgs> DataReceived;

       
        public void Start(string address, int numWorkerThreads)
        {
            listener = new HttpListener();
        
            listener.Prefixes.Add(address);

            scheduler = new Semaphore(numWorkerThreads, numWorkerThreads);

            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Failed to start listener for {0} make sure that you have admin priviliges",address),ex);
            }
           
            new Thread(HttpServer).Start();
        }

        public void Dispose()
        {
            listener.Stop();
            scheduler.Dispose();
        }

        public void Handle(HttpListenerContext ctx)
        {
            try
            {
                if(!IsGatewayRequest(ctx.Request))
                {
                    //there will always be a responder
                    Configure.Instance.Builder.Build<IHttpResponder>().Handle(ctx);
                    return;
                }

                DataReceived(this, new DataReceivedOnChannelArgs
                                       {
                                           Headers = GetHeaders(ctx),
                                           Data = GetMessageStream(ctx)
                                       });
                ReportSuccess(ctx);

                Logger.Debug("Http request processing complete.");
            }
            catch (HttpChannelException ex)
            {
                CloseResponseAndWarn(ctx, ex.Message, ex.StatusCode);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error", ex);
                CloseResponseAndWarn(ctx, "Unexpected server error", 502);
            }
        }

        static MemoryStream GetMessageStream(HttpListenerContext ctx)
        {
            if(ctx.Request.QueryString.AllKeys.Contains("Message"))
            {
                var message = HttpUtility.UrlDecode(ctx.Request.QueryString["Message"]);

                return new MemoryStream(Encoding.UTF8.GetBytes(message));
            }
                
            var streamToReturn = new MemoryStream();

            ctx.Request.InputStream.CopyTo_net35(streamToReturn, MaximumBytesToRead);
            streamToReturn.Position = 0;
            return streamToReturn;
        }

        bool IsGatewayRequest(HttpListenerRequest request)
        {
            return request.Headers.AllKeys.Contains(GatewayHeaders.CallTypeHeader) ||
                   request.Headers.AllKeys.Contains(GatewayHeaders.CallTypeHeader.ToLower()) ||
                   request.QueryString[GatewayHeaders.CallTypeHeader] != null;
        }


        static IDictionary<string,string> GetHeaders(HttpListenerContext ctx)
        {
            var headers = new Dictionary<string,string>(StringComparer.CurrentCultureIgnoreCase);

            foreach (string header in ctx.Request.Headers.Keys)
                headers.Add(HttpUtility.UrlDecode(header), HttpUtility.UrlDecode(ctx.Request.Headers[header]));
            
            foreach (string header in ctx.Request.QueryString.Keys)
                headers[HttpUtility.UrlDecode(header)] = HttpUtility.UrlDecode(ctx.Request.QueryString[header]);

            return headers;
        }


        void HttpServer()
        {
            while (true)
            {
                try
                {
                    scheduler.WaitOne();
                    var ctx = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(x => {
                        try
                        {
                            Handle(ctx);
                        }
                        finally
                        {
                            scheduler.Release();
                        }
                    });
                }
                catch (Exception e)
                {
                    Logger.Error("HttpListener failed", e);
                    break;
                }
            }
        }

        static void ReportSuccess(HttpListenerContext ctx)
        {
            Logger.Debug("Sending HTTP 200 response.");

            ctx.Response.StatusCode = 200;
            ctx.Response.StatusDescription = "OK";

          
            WriteData(ctx,"OK");
        }

        static void WriteData(HttpListenerContext ctx,string status)
        {
            var str = status;

            var jsonp = ctx.Request.QueryString["callback"];
            if (string.IsNullOrEmpty(jsonp) == false)
            {
                str = jsonp + "({ status: '" + str + "'})";
                ctx.Response.AddHeader("Content-Type", "application/javascript; charset=utf-8");
            }
            else
            {
                ctx.Response.AddHeader("Content-Type", "application/json; charset=utf-8");
            }
            ctx.Response.Close(Encoding.ASCII.GetBytes(str), false);
        }
        
        static void CloseResponseAndWarn(HttpListenerContext ctx, string warning, int statusCode)
        {
            try
            {
                Logger.WarnFormat("Cannot process HTTP request from {0}. Reason: {1}.", ctx.Request.RemoteEndPoint, warning);
                ctx.Response.StatusCode = statusCode;
                ctx.Response.StatusDescription = warning;

                WriteData(ctx, warning);
            }
            catch (Exception e)
            {
                Logger.Error("Could not return warning to client.", e);
            }
        }

        HttpListener listener;
        Semaphore scheduler;
        
        const int MaximumBytesToRead = 100000;
        
        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
    }
}
