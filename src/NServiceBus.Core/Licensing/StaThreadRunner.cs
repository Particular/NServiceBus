namespace NServiceBus.Licensing
{
    using System;
    using System.Threading;

    static class StaThreadRunner
    {
        public static T ShowDialogInSTA<T>(Func<T> func)
        {
            var result = default(T);

            var thread = new Thread(() =>
            {
                result = func();
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return result;
        }
    }
}