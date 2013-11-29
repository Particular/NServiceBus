namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class BehaviorList<TContext> where TContext : BehaviorContext
    {
        public List<Type> InnerList { get; set; }

        public BehaviorList()
        {
            InnerList = new List<Type>();
        }

        public void Add<T>() where T : IBehavior<TContext> 
        {
            InnerList.Add(typeof(T));
        }

        public bool Remove<T>() where T : IBehavior<TContext>
        {
            return InnerList.Remove(typeof(T));
        }

        public bool Replace<TExisting, TToReplace>()
            where TExisting : IBehavior<TContext>
            where TToReplace : IBehavior<TContext>
        {
            var indexOf = InnerList.IndexOf(typeof(TExisting));
            if (indexOf > -1)
            {
                InnerList[indexOf] = typeof(TToReplace);
                return true;
            }
            return false;
        }

        public bool InsertAfter<TExisting,TToAdd>()
            where TExisting : IBehavior<TContext>
            where TToAdd : IBehavior<TContext>
        {
            for (var index = 0; index < InnerList.Count; index++)
            {
                var type = InnerList[index];
                if (type == typeof(TExisting))
                {
                    InnerList.Insert(index + 1, typeof(TToAdd));
                    return true;
                }
            }
            return false;
        }

        public bool InsertBefore<TExisting, TToAdd>()
            where TExisting : IBehavior<TContext>
            where TToAdd : IBehavior<TContext>
        {
            for (var index = 0; index < InnerList.Count; index++)
            {
                var type = InnerList[index];
                if (type == typeof(TExisting))
                {
                    InnerList.Insert(index, typeof(TToAdd));
                    return true;
                }
            }
            return false;
        }
    }
}