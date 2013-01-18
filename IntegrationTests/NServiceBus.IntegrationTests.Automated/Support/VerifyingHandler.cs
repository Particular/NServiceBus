namespace NServiceBus.IntegrationTests.Automated.Support
{
    using System.Collections.Generic;

    //public class VerifyingHandler<T> : IHandleMessages<T>
    //{
    //    public static bool WasCalled
    //    {
    //        get
    //        {
    //            return Message != null;
    //        }

    //    }


    //    public static T Message { get; set; }
    //    public static IMessageContext Context { get; set; }

    //    public static IDictionary<string, string> Headers
    //    {
    //        get { return Context.Headers; }
    //    }

    //    public void Handle(T message)
    //    {
    //        Message = message;

    //        Context = Bus.CurrentMessageContext;
    //    }

    //    public IBus Bus { get; set; }
    //}
}