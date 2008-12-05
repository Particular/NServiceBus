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
            this.expires = DateTime.SpecifyKind(expiration, DateTimeKind.Utc);
            this.SagaId = saga.Id;
            this.State = state;
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
            this.SagaId = saga.Id;
            this.ClearTimeout = clear;
        }

        private DateTime expires;

		/// <summary>
		/// Gets/sets the date and time at which the timeout message is due to expire.
		/// </summary>
        public DateTime Expires
        {
            get { return expires; }
            set { expires = DateTime.SpecifyKind(value, DateTimeKind.Utc); }
        }

		/// <summary>
		/// Gets/sets the Id of the workflow the TimeoutMessage is connected to.
		/// </summary>
        public Guid SagaId { get; set; }

        /// <summary>
        /// Should be used for data to differentiate between various
        /// timeout occurrences.
        /// </summary>
        public object State { get; set; }

        /// <summary>
        /// When true, signals to the timeout manager that all other <see cref="TimeoutMessage"/> objects
        /// can be cleared for the given <see cref="SagaId"/>.
        /// </summary>
        public bool ClearTimeout { get; set; }

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
