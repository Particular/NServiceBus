namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;

    public abstract class RegisterBehavior
    {
        protected RegisterBehavior(string id, Type behavior, string description)
        {
            if (behavior == null)
            {
                throw new ArgumentNullException("id");
            }

            if (behavior.IsAssignableFrom(iBehaviourType))
            {
                throw new ArgumentException("Needs to implement IBehavior<TContext>", "behavior");
            }

            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            if (String.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException("description");
            }

            BehaviorType = behavior;
            Id = id;
            Description = description;
        }

        public string Id { get; private set; }
        public string Description { get; internal set; }
        internal IList<string> Befores { get; private set; }
        internal IList<string> Afters { get; private set; }
        public Type BehaviorType { get; internal set; }

        public void InsertBefore(string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            if (Befores == null)
            {
                Befores = new List<string>();
            }

            Befores.Add(id);
        }

        public void InsertAfter(string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            if (Afters == null)
            {
                Afters = new List<string>();
            }

            Afters.Add(id);
        }

        internal static RegisterBehavior Create(string id, Type behavior, string description)
        {
            return new DefaultRegisterBehavior(behavior, id, description);
        }

        static Type iBehaviourType = typeof(IBehavior<>);

        class DefaultRegisterBehavior : RegisterBehavior
        {
            public DefaultRegisterBehavior(Type behavior, string id, string description) : base(id, behavior, description)
            {
            }
        }
    }
}