using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Next.Abstractions;

namespace RabbitMQ.Next.Transport.Events
{
    internal class EventSource<TEventArgs> : IEventSource<TEventArgs>
    {
        private readonly List<ISubscription<TEventArgs>> subscriptions = new List<ISubscription<TEventArgs>>();
        private readonly SemaphoreSlim sync = new SemaphoreSlim(1, 1);

        public IDisposable Subscribe<TSubscriber>(TSubscriber subscriber, Func<TSubscriber, Func<TEventArgs, ValueTask>> handlerSelector)
            where TSubscriber : class
        {
            var subscription = new Subscription<TSubscriber, TEventArgs>(subscriber, handlerSelector);

            this.sync.WaitAsync().GetAwaiter().GetResult();
            try
            {
                this.subscriptions.Add(subscription);
            }
            finally
            {
                this.sync.Release();
            }

            return subscription;
        }

        public async ValueTask InvokeAsync(TEventArgs eventArgs)
        {
            await this.sync.WaitAsync();
            try
            {
                for (var i = this.subscriptions.Count - 1; i >= 0; i--)
                {
                    var result = await this.subscriptions[i].HandleAsync(eventArgs);
                    if (!result)
                    {
                        this.subscriptions.RemoveAt(i);
                    }
                }
            }
            finally
            {
                this.sync.Release();
            }
        }
    }
}