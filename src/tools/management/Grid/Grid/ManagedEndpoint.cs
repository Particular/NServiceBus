using System;
using System.Collections.Generic;

namespace Grid
{
    [Serializable]
    public class ManagedEndpoint
    {
        private string queue;
        public string Queue
        {
            get {return queue;}
            set{queue = value;}
        }

        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private int numberOfMessages;
        public int NumberOfMessages
        {
            get
            {
                lock(this)
                    return numberOfMessages;
            }
        }

        private List<Worker> workers = new List<Worker>();
        public IList<Worker> Workers
        {
            get { return workers; }
        }

        public override string ToString()
        {
            return string.Format("{0} : {1}", this.name, this.queue);
        }

        public void SetNumberOfMessages(int number)
        {
            lock (this)
                this.numberOfMessages = number;
        }

        public TimeSpan AgeOfOldestMessage { get; set; }

    }
}
