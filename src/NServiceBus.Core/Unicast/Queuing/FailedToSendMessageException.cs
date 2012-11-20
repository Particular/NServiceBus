namespace NServiceBus.Unicast.Queuing
{
    using System;

    [Serializable]
    public class FailedToSendMessageException : Exception
    {
      public FailedToSendMessageException() { }
      public FailedToSendMessageException( string message ) : base( message ) { }
      public FailedToSendMessageException( string message, Exception inner ) : base( message, inner ) { }
      protected FailedToSendMessageException( 
	    System.Runtime.Serialization.SerializationInfo info, 
	    System.Runtime.Serialization.StreamingContext context ) : base( info, context ) { }
    }
}
