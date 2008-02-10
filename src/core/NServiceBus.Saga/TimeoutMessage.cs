using System;

namespace NServiceBus.Saga
{
	/// <summary>
	/// A message to signal a saga that a reminder was set.
	/// </summary>
    [Serializable]
    [Recoverable]
    public class TimeoutMessage : ISagaMessage
    {
        public TimeoutMessage() { }
        public TimeoutMessage(DateTime expiration, ISagaEntity saga, object state)
        {
            this.expires = expiration;
            this.sagaId = saga.Id;
            this.state = state;
        }

	    public TimeoutMessage(TimeSpan expireIn, ISagaEntity saga, object state) :
	        this(DateTime.Now + expireIn, saga, state)
	    {
	        
	    }

        /// <summary>
        /// Signal to the timeout manager that all other <see cref="TimeoutMessage"/>
        /// objects can be cleared for the given <see cref="saga"/>.
        /// </summary>
        /// <param name="saga"></param>
        /// <param name="clear"></param>
        public TimeoutMessage(ISagaEntity saga, bool clear)
        {
            this.sagaId = saga.Id;
            this.clearTimeout = clear;
        }

        private DateTime expires;

		/// <summary>
		/// Gets/sets the date and time at which the timeout message is due to expire.
		/// </summary>
        public DateTime Expires
        {
            get { return expires; }
            set { expires = value; }
        }

        private Guid sagaId;

		/// <summary>
		/// Gets/sets the Id of the workflow the TimeoutMessage is connected to.
		/// </summary>
        public Guid SagaId
        {
            get { return sagaId; }
            set { sagaId = value; }
        }

        private object state;

        /// <summary>
        /// Contains the object passed as the state parameter
        /// to the ExpireIn method of <see cref="Reminder"/>.
        /// </summary>
        public object State
        {
            get { return state; }
            set { state = value; }
        }

        /// <summary>
        /// When true, signals to the timeout manager that all other <see cref="TimeoutMessage"/> objects
        /// can be cleared for the given <see cref="SagaId"/>.
        /// </summary>
	    public bool ClearTimeout
	    {
	        get { return clearTimeout; }
	        set { clearTimeout = value; }
	    }

	    private bool clearTimeout;

		/// <summary>
		/// Gets whether or not the TimeoutMessage has expired.
		/// </summary>
		/// <returns>true if the message has expired, otherwise false.</returns>
        public bool HasNotExpired()
        {
            return DateTime.Now < this.expires;
        }
    }
}
