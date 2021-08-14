using System;
using System.Threading.Tasks;
using RabbitMQ.Next.Abstractions.Messaging;

namespace RabbitMQ.Next.Publisher.Abstractions
{
    internal class ReturnedMessageDelegateHandler : IReturnedMessageHandler
    {
        private Func<ReturnedMessage, IMessageProperties, IContentAccessor, ValueTask<bool>> wrapped;

        public ReturnedMessageDelegateHandler(Func<ReturnedMessage, IMessageProperties, IContentAccessor, ValueTask<bool>> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            this.wrapped = handler;
        }

        public void Dispose()
        {
            this.wrapped = null;
        }

        public ValueTask<bool> TryHandleAsync(ReturnedMessage message, IMessageProperties properties, IContentAccessor content)
        {
            if (this.wrapped == null)
            {
                return new ValueTask<bool>(false);
            }

            return this.wrapped(message, properties, content);
        }
    }
}