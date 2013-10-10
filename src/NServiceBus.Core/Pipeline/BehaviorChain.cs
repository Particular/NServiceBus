namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class BehaviorChain : IEnumerable<Type>
    {
        readonly List<Type> behaviorTypes = new List<Type>();

        public void Add(Type type)
        {
            behaviorTypes.Add(type);
        }

        public void Invoke(TransportMessage incomingTransportMessage)
        {
            var head = GenerateBehaviorChain();
            var context = new SimpleContext(incomingTransportMessage);

            try
            {
                Console.WriteLine(@"Invoking chain:
{0}", ToString());

                head.Invoke(context);
            }
            catch (Exception exception)
            {
                throw new ApplicationException(
                    string.Format("An error occurred while attempting to invoke the following behavior chain: {0}",
                                  string.Join(" -> ", behaviorTypes.Select(t => t.Name))), exception);
            }
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine,
                               behaviorTypes.Select((type, idx) => new string(' ', idx*2) + " -> " + type.Name));
        }

        IBehavior GenerateBehaviorChain()
        {
            var clonedList = behaviorTypes.ToList();
            clonedList.Reverse();

            // start with the end
            IBehavior behavior = new Terminator();

            // traverse the pipeline in reverse order, tying each behavior to the following
            foreach (var type in clonedList)
            {
                var next = behavior;
                behavior = CreateLazyFor(type);
                behavior.Next = next;
            }
            return behavior;
        }

        class SimpleContext : IBehaviorContext
        {
            public SimpleContext(TransportMessage transportMessage)
            {
                Set(transportMessage);
            }

            public TransportMessage TransportMessage
            {
                get { return Get<TransportMessage>(); }
            }

            public object Message
            {
                get { return stash["NServiceBus.Message"]; }
            }

            readonly Dictionary<string, object> stash = new Dictionary<string, object>();

            public T Get<T>()
            {
                return stash.ContainsKey(typeof(T).FullName)
                           ? (T)stash[typeof(T).FullName]
                           : default(T);
            }

            public void Set<T>(T t)
            {
                stash[typeof(T).FullName] = t;
            }
        }

        IBehavior CreateLazyFor(Type behaviorType)
        {
            try
            {
                var wrapperType = typeof(LazyBehavior<>).MakeGenericType(behaviorType);
                var instance = Activator.CreateInstance(wrapperType, new object[] { Configure.Instance.Builder });
                return (IBehavior)instance;
            }
            catch (Exception exception)
            {
                throw new ApplicationException(
                    string.Format("An error occurred while attempting to create an instance of {0} closed with {1}",
                                  typeof(LazyBehavior<>), behaviorType), exception);
            }
        }

        class Terminator : IBehavior
        {
            public IBehavior Next
            {
                get { throw new InvalidOperationException("Can't get next on a terminator - this behavior terminates the pipeline"); }
                set { throw new InvalidOperationException("Can't set next on a terminator - this behavior terminates the pipeline"); }
            }

            public void Invoke(IBehaviorContext context)
            {
            }
        }

        public IEnumerator<Type> GetEnumerator()
        {
            return behaviorTypes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}