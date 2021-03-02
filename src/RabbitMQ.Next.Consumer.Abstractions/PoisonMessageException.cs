using System;

namespace RabbitMQ.Next.Consumer.Abstractions
{
    public class PoisonMessageException : Exception
    {
        public PoisonMessageException(IDeliveredMessage message, IContent content, Exception inner)
            : base("Failed to handle the delivered message" ,inner)
        {
            this.DeliveredMessage = message;
            this.Content = content;
        }

        public IDeliveredMessage DeliveredMessage { get; }

        public IContent Content { get; }
    }
}