namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    // Taken from https://devblogs.microsoft.com/pfxteam/building-async-coordination-primitives-part-2-asyncautoresetevent/
    class AsyncAutoResetEvent
    {
        public Task WaitAsync()
        {
            lock (m_waits)
            {
                if (m_signaled)
                {
                    m_signaled = false;
                    return s_completed;
                }
                else
                {
                    var tcs = new TaskCompletionSource<bool>();
                    m_waits.Enqueue(tcs);
                    return tcs.Task;
                }
            }
        }

        public void Set()
        {
            TaskCompletionSource<bool> toRelease = null;
            lock (m_waits)
            {
                if (m_waits.Count > 0)
                {
                    toRelease = m_waits.Dequeue();
                }
                else if (!m_signaled)
                {
                    m_signaled = true;
                }
            }
            if (toRelease != null)
            {
                toRelease.SetResult(true);
            }
        }

        static readonly Task s_completed = Task.FromResult(true);
        readonly Queue<TaskCompletionSource<bool>> m_waits = new Queue<TaskCompletionSource<bool>>();
        bool m_signaled;
    }
}
