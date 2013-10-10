namespace NServiceBus.Pipeline.Behaviors
{
    using System.Reflection;
    using Logging;

    /// <summary>
    /// Arguably not the most interesting behavior, but let's just handle logging like this too
    /// </summary>
    public class MessageHandlingLoggingBehavior : IBehavior
    {
        static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public IBehavior Next { get; set; }
        
        public void Invoke(IBehaviorContext context)
        {
            var msg = context.TransportMessage;
            Log.Debug("Received message with ID " + msg.Id + " from sender " + msg.ReplyToAddress);

            Next.Invoke(context);

            Log.Debug("Finished handling message.");
        }
    }
}