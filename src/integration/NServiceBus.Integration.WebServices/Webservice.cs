using System;
using System.ComponentModel;
using System.Web.Services;

namespace NServiceBus
{
    /// <summary>
    /// Base class for writing web services that serve as a bridge to a messaging endpoint
    /// </summary>
    /// <typeparam name="TRequest">The request message type</typeparam>
    /// <typeparam name="TResponse">The response code enumeration type</typeparam>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public abstract class Webservice<TRequest, TResponse> : WebService where TRequest : IMessage
    {
        /// <summary>
        /// Static constructor that checks that the type TResponse is an enum.
        /// </summary>
        static Webservice()
        {
            if (!typeof(TResponse).IsEnum)
                throw new InvalidOperationException(typeof(TResponse).FullName + " must be an enum representing error codes returned by the server.");
        }

        /// <summary>
        /// Constructor to initialize bus.
        /// </summary>
        protected Webservice()
        {
            bus = Configure.Instance.Builder.Build<IBus>();            
        }

        /// <summary>
        /// Initiates the sending of the message.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cb"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [WebMethod]
        public IAsyncResult BeginProcess(TRequest request, AsyncCallback cb, object state)
        {
            return bus.Send(request).Register(cb, state);
        }

        /// <summary>
        /// Handles the response from the server.
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        [WebMethod]
        public TResponse EndProcess(IAsyncResult ar)
        {
            var completionResult = ar.AsyncState as CompletionResult;

            if (completionResult == null)
                throw new InvalidOperationException("Response returned from server did not contain a CompletionResult.");

            return (TResponse)Enum.ToObject(typeof(TResponse), completionResult.ErrorCode);
        }

        private readonly IBus bus;
    }
}
