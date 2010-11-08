using System;
using StructureMap;
using StructureMap.Pipeline;

namespace NServiceBus.ObjectBuilder.StructureMap
{
    public class NServiceBusThreadLocalStorageLifestyle : IMessageModule,ILifecycle
    {
        public void EjectAll()
        {
            FindCache().DisposeAndClear();
        }

        public IObjectCache FindCache()
        {
            guaranteeHashExists();
            return cache;
        }

        public void HandleBeginMessage() { }

        public void HandleEndMessage()
        {
            EjectAll();
        }

        public void HandleError() { }


        public string Scope
        {
            get
            {
                return typeof (NServiceBusThreadLocalStorageLifestyle).Name;
            }
        }

        private void guaranteeHashExists()
        {
            if (cache == null)
            {
                lock (locker)
                {
                    if (cache == null)
                    {
                        cache = new MainObjectCache();
                    }
                }
            }
        }

        [ThreadStatic]
        private static MainObjectCache cache;
        private readonly object locker = new object();

    }
    
       
}