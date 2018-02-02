using System;

namespace truepele.Threading
{
    public static class EventHandlerExtensions
    {
        public static void BeginRaiseEvent<TEventArgs>(this EventHandler<TEventArgs> instance, object sender,
            TEventArgs eventArgs)
        {
            var invocationList = instance?.GetInvocationList();
            if (invocationList == null || invocationList.Length == 0)
            {
                return;
            }

            foreach (EventHandler<TEventArgs> handler in invocationList)
            {
                handler.BeginInvoke(sender, eventArgs, handler.EndInvoke, null);
            }
        }
    }
}