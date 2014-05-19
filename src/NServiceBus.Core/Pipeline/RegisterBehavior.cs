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
        internal IList<BeforeRegistration> Befores { get; private set; }
        internal IList<AfterRegistration> Afters { get; private set; }
        public Type BehaviorType { get; internal set; }

        public void InsertBefore(string id, bool ignoreIfNonExisting = false)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            if (Befores == null)
            {
                Befores = new List<BeforeRegistration>();
            }

            Befores.Add(new BeforeRegistration(id,ignoreIfNonExisting));
        }

        public void InsertAfter(string id, bool ignoreIfNonExisting = false)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            if (Afters == null)
            {
                Afters = new List<AfterRegistration>();
            }

            Afters.Add(new AfterRegistration(id,ignoreIfNonExisting));
        }

        internal static RegisterBehavior Create(string id, Type behavior, string description)
        {
            return new DefaultRegisterBehavior(behavior, id, description);
        }

        static Type iBehaviourType = typeof(IBehavior<>);

        class DefaultRegisterBehavior : RegisterBehavior
        {
            public DefaultRegisterBehavior(Type behavior, string id, string description)
                : base(id, behavior, description)
            {
            }
        }
    }

    class AfterRegistration
    {
        public string Id { get; set; }
        public bool IgnoreIfNonExisting { get; set; }

        public AfterRegistration(string id, bool ignoreIfNonExisting)
        {
            Id = id;
            IgnoreIfNonExisting = ignoreIfNonExisting;
        }
    }

    class BeforeRegistration
    {
        public string Id { get; set; }
        public bool IgnoreIfNonExisting { get; set; }

        public BeforeRegistration(string id, bool ignoreIfNonExisting)
        {
            Id = id;
            IgnoreIfNonExisting = ignoreIfNonExisting;
        }
    }
}