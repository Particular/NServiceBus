namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
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

        public void Replace<TExisting, TToReplace>()
            where TExisting : IBehavior<TContext>
            where TToReplace : IBehavior<TContext>
        {
            var indexOf = InnerList.IndexOf(typeof(TExisting));
            if (indexOf > -1)
            {
                InnerList[indexOf] = typeof(TToReplace);
                return;
            }
            throw new Exception(string.Format("Could not replace since '{0}' does not exist.", typeof(TExisting).Name));
        }

        public void InsertAfter<TExisting, TToAdd>()
            where TExisting : IBehavior<TContext>
            where TToAdd : IBehavior<TContext>
        {
            for (var index = 0; index < InnerList.Count; index++)
            {
                var type = InnerList[index];
                if (type == typeof(TExisting))
                {
                    InnerList.Insert(index + 1, typeof(TToAdd));
                    return;
                }
            }
            throw new Exception(string.Format("Could not InsertAfter since '{0}' does not exist.", typeof(TExisting).Name));
        }

        public void InsertBefore<TExisting, TToAdd>()
            where TExisting : IBehavior<TContext>
            where TToAdd : IBehavior<TContext>
        {
            for (var index = 0; index < InnerList.Count; index++)
            {
                var type = InnerList[index];
                if (type == typeof(TExisting))
                {
                    InnerList.Insert(index, typeof(TToAdd));
                    return;
                }
            }
            throw new Exception(string.Format("Could not InsertBefore  since '{0}' does not exist.", typeof(TExisting).Name));
        }
    }
}